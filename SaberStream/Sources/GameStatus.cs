using Newtonsoft.Json.Linq;
using SaberStream.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SaberStream.Sources
{
    public static class GameStatus
    {
        private const int BUFFER_SIZE = 8192;
        private const bool DEBUG = false;
        private static string GameSocketURL = "ws://localhost:6557/socket";

        private static ClientWebSocket? WebSocket;
        private static CancellationTokenSource? CancelSource;

        public static List<PerformanceEntry> CurrentPerformance { get; private set; } = new();
        public static MapInfoPlaying? CurrentMap { get; private set; }
        private static string? PreviousMapHash;

        public static void Start(string? socket)
        {
            if (socket != null) { GameSocketURL = socket; }
            _ = Connect();
        }

        public static void Stop()
        {
            try { Disconnect().Wait(); }
            catch (Exception) { }
        }

        private static async Task Connect()
        {
            Console.WriteLine("Connecting to game...");
            if (WebSocket != null)
            {
                if (WebSocket.State == WebSocketState.Open) { return; }
                else { WebSocket.Dispose(); }
            }
            WebSocket = new();
            if (CancelSource != null) { CancelSource.Dispose(); }
            CancelSource = new();

            await WebSocket.ConnectAsync(new Uri("ws://localhost:6557/socket"), CancelSource.Token);
            await Task.Factory.StartNew(ReceiveLoop, CancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static async Task Disconnect()
        {
            if (WebSocket == null) { return; }
            if (WebSocket.State == WebSocketState.Open)
            {
                CancelSource?.CancelAfter(TimeSpan.FromSeconds(2));
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            WebSocket.Dispose();
            WebSocket = null;
            CancelSource?.Dispose();
            CancelSource = null;
        }

        private static async Task ReceiveLoop()
        {
            Console.WriteLine("Connected to game.");
            CancellationToken LoopToken = CancelSource!.Token;
            MemoryStream? OutputStream = null;
            byte[] Buffer = new byte[BUFFER_SIZE];
            try
            {
                while (!LoopToken.IsCancellationRequested)
                {
                    OutputStream = new(BUFFER_SIZE);
                    WebSocketReceiveResult? Result;
                    do
                    {
                        Result = await WebSocket!.ReceiveAsync(Buffer, CancelSource.Token);
                        if (Result.MessageType != WebSocketMessageType.Close) { OutputStream.Write(Buffer, 0, Result.Count); }
                    }
                    while (!Result.EndOfMessage);
                    if (Result.MessageType == WebSocketMessageType.Close) { break; }
                    OutputStream.Position = 0;
                    ProcessResponse(OutputStream);
                    OutputStream.Dispose();
                }
            }
            catch (TaskCanceledException) { }
            finally { OutputStream?.Dispose(); }
        }

        private static void ProcessResponse(Stream inputStream)
        {
            JObject JSON = JObject.Parse(new StreamReader(inputStream).ReadToEnd());
            string? EventType = JSON.Value<string>("event");
            JToken? Status = JSON["status"];
            JToken? PerfNode = Status?["performance"];
            if (EventType == null || Status == null || PerfNode == null) { return; }

            if (DEBUG) { Console.WriteLine($"Received event '{EventType}'"); }
            switch (EventType)
            {
                case "hello": // Connection is established
                    break;

                case "songStart": // A beatmap is started
                    ProcessStart(Status);
                    break;

                case "finished": // A beatmap is finished sucessfully
                    ProcessFinish(Status, true);
                    break;

                case "failed": // A beatmap is failed
                    ProcessFinish(Status, false);
                    break;

                case "menu": // The menu is displayed
                    StateTransition?.Invoke(typeof(GameStatus), new StateTransitionEventArgs(false));
                    break;

                case "obstacleEnter": // Combo is broken
                case "bombCut":
                case "noteMissed":
                    ProcessMistake(PerfNode);
                    break;

                case "noteCut":
                    ProcessCut(PerfNode);
                    break;
            }
        }

        /// <summary>Makes necessary changes, and fires off events for a song being started.</summary>
        /// <param name="status">The status object from the game</param>
        private static void ProcessStart(JToken status)
        {
            MapInfoPlaying? Beatmap = ParseMapInfo(status["beatmap"]);
            CurrentMap = Beatmap;
            if (Beatmap != null)
            {
                SongStarted?.Invoke(typeof(GameStatus), new SongStartedEventArgs(Beatmap, Beatmap.Hash == PreviousMapHash));
                PreviousMapHash = Beatmap.Hash;
            }
            StateTransition?.Invoke(typeof(GameStatus), new StateTransitionEventArgs(true));
        }

        /// <summary>Makes necessary changes, and fires off events for a song stopping.</summary>
        /// <param name="status">The status object from the game</param>
        /// <param name="success">Whether the song was finished successfully</param>
        private static void ProcessFinish(JToken status, bool success)
        {
            SongEnded?.Invoke(typeof(GameStatus), new SongEndedEventArgs(success));
            StateTransition?.Invoke(typeof(GameStatus), new StateTransitionEventArgs(false));
            CurrentMap = null;
        }

        /// <summary>Makes necessary changes, and fires off events for a player mistake resulting in a broken combo.</summary>
        /// <param name="perf">The status->performance object from the game</param>
        private static void ProcessMistake(JToken perf)
        {
            float SongPosition = perf.Value<int>("songPosition");
            lock (CurrentPerformance)
            {
                if (CurrentPerformance.Count == 0 || CurrentPerformance[^1].WasCorrect)
                {
                    CurrentPerformance.Add(new()
                    {
                        WasCorrect = false,
                        NoteCount = 1,
                        LastActionTime = SongPosition
                    });
                }
                else
                {
                    CurrentPerformance[^1].NoteCount++;
                    CurrentPerformance[^1].LastActionTime = SongPosition;
                }
            }
            Mistake?.Invoke(typeof(GameStatus), new MapProgressEventArgs(CurrentPerformance[^1].NoteCount, SongPosition));
        }

        /// <summary>Makes necessary changes, and fires off events for a player cutting a note correctly.</summary>
        /// <param name="perf">The status->performance object from the game</param>
        private static void ProcessCut(JToken perf)
        {
            float SongPosition = perf.Value<int>("songPosition");
            if (CurrentPerformance.Count == 0 || !CurrentPerformance[^1].WasCorrect)
            {
                CurrentPerformance.Add(new()
                {
                    WasCorrect = true,
                    NoteCount = 1,
                    LastActionTime = SongPosition
                });
            }
            else
            {
                CurrentPerformance[^1].NoteCount++;
                CurrentPerformance[^1].LastActionTime = SongPosition;
            }
            Cut?.Invoke(typeof(GameStatus), new MapProgressEventArgs(CurrentPerformance[^1].NoteCount, SongPosition));
        }

        /// <summary>Parses map info out of the status->beatmap section</summary>
        /// <param name="root">The status section of the event</param>
        /// <returns>Map information if it was successfully parsed, otherwise null</returns>
        private static MapInfoPlaying? ParseMapInfo(JToken? root)
        {
            if (root == null || !root.HasValues) { return null; }

            string? CoverB64 = root.Value<string>("songCover");
            DifficultyInfo CurrentDifficulty = new(DifficultyUtil.Parse(root.Value<string>("difficulty")))
            {
                NoteCount = root.Value<int>("notesCount"),
                BombCount = root.Value<int>("bombsCount"),
                WallCount = root.Value<int>("obstaclesCount")
            };

            MapInfoPlaying Result = new(null)
            {
                SongName = root.Value<string>("songName"),
                SongSubName = root.Value<string>("songSubName"),
                SongAuthor = root.Value<string>("songAuthorName"),
                MapAuthor = root.Value<string>("levelAuthorName"),
                CoverArt = CoverB64 == null ? null : Convert.FromBase64String(CoverB64),
                Hash = root.Value<string>("songHash"),
                MapFolder = root.Value<string>("levelFileLocation"),
                BPM = root.Value<float>("songBPM"),
                NJS = root.Value<float>("noteJumpSpeed"),
                Length = TimeSpan.FromMilliseconds(root.Value<float>("length")),
                SongPosition = TimeSpan.FromMilliseconds(root.Value<float>("songPosition")),
                DifficultyPlaying = CurrentDifficulty
            };
            return Result;
        }

        /// <summary>Parses performance info out of the status->performance section</summary>
        /// <param name="root">The status section of the event</param>
        /// <returns>Performance information if it was successfully parsed, otherwise null</returns>
        private static Performance? ParsePerformance(JToken? root)
        {
            if (root == null || !root.HasValues) { return null; }
            Performance Result = new()
            {
                Score = root.Value<int>("score"),
                Rank = root.Value<string>("rank"),
                NotesPassed = root.Value<int>("passedNotes"),
                NotesHit = root.Value<int>("hitNotes"),
                Combo = root.Value<int>("combo"),
                MaxCombo = root.Value<int>("maxCombo")
            };
            return Result;
        }

        // Events

        public class SongStartedEventArgs : EventArgs
        {
            public MapInfoPlaying Beatmap { get; set; }
            public bool Retry { get; set; }
            
            public SongStartedEventArgs(MapInfoPlaying map, bool retry)
            {
                this.Beatmap = map;
                this.Retry = retry;
            }
        }
        public delegate void SongStartedHandler(object? sender, SongStartedEventArgs evt);
        public static event SongStartedHandler? SongStarted;

        public class SongEndedEventArgs : EventArgs
        {
            public bool Succeeded { get; set; }
            public SongEndedEventArgs(bool succeeeded) { this.Succeeded = succeeeded; }
        }
        public delegate void SongEndedHandler(object? sender, SongEndedEventArgs evt);
        public static event SongEndedHandler? SongEnded;

        public class StateTransitionEventArgs : EventArgs
        {
            public bool IsPlayingSong { get; set; }
            public StateTransitionEventArgs(bool isPlayingSong) { this.IsPlayingSong = isPlayingSong; }
        }
        public delegate void StateTransitionHandler(object? sender, StateTransitionEventArgs evt);
        public static event StateTransitionHandler? StateTransition;

        public class MapProgressEventArgs : EventArgs
        {
            public int SequenceNumber { get; set; }
            public float SongTime { get; set; }
            
            public MapProgressEventArgs(int sequence, float time)
            {
                this.SequenceNumber = sequence;
                this.SongTime = time;
            }
        }
        public delegate void MapProgressHandler(object? sender, MapProgressEventArgs evt);
        public static event MapProgressHandler? Mistake;
        public static event MapProgressHandler? Cut;
    }
}
