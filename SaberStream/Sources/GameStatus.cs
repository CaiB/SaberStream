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

        public static List<HistoryEntry> MapHistory { get; private set; } = new();
        public static MapInfoPlaying? CurrentMap { get; private set; }
        public static MapInfoPlaying? PreviousMap { get; private set; }
        private static string? PreviousMapHash;
        private static bool JustFailed = false;

        /// <summary>This is set to true upon a fail or completion, but not in the event of a simple menu exit.</summary>
        private static bool WasCaughtExit = false;

        /// <summary>Attempts to connect to the game's WebSocket to receive status updates.</summary>
        /// <param name="socket">The WebSocket URI to connect to</param>
        public static void Start(string? socket)
        {
            if (socket != null) { GameSocketURL = socket; }
            _ = Connect();
        }

        /// <summary>Disconnects from the game's WebSocket.</summary>
        public static void Stop()
        {
            try { Disconnect().Wait(); }
            catch (Exception) { }
        }

        /// <summary>Connects to the socket, and starts the receive loop.</summary>
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

        /// <summary>Terminates the socket connection, and stops the receive loop.</summary>
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

        /// <summary>Processes incoming data from the socket, and dispatches data once received.</summary>
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

        /// <summary>Reads a message as received from the socket, parses the data, and sends it to the corresponding listeners.</summary>
        /// <param name="inputStream">A stream containing the socket message data</param>
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
                    if (!WasCaughtExit) { ProcessFinish(Status, false); }
                    StateTransition?.Invoke(typeof(GameStatus), new StateTransitionEventArgs(false));
                    WasCaughtExit = false;
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
                bool IsRetry = Beatmap.Hash == PreviousMapHash;
                SongStarted?.Invoke(typeof(GameStatus), new SongStartedEventArgs(Beatmap, IsRetry));
                PreviousMapHash = Beatmap.Hash;
                if (IsRetry)
                {
                    JustFailed = true;
                }
                else
                {
                    lock (MapHistory)
                    {
                        MapHistory = new();
                        MapHistory.Add(new()
                        {
                            Type = HistoryType.Join,
                            NoteCount = 1,
                            EndTime = TimeSpan.Zero,
                            NewDifficulty = Beatmap.DifficultyPlaying?.Difficulty ?? Difficulty.None
                        });
                    }
                }
            }
            StateTransition?.Invoke(typeof(GameStatus), new StateTransitionEventArgs(true));
        }

        /// <summary>Makes necessary changes, and fires off events for a song stopping.</summary>
        /// <remarks>This is not called if the player exits via menu.</remarks>
        /// <param name="status">The status object from the game</param>
        /// <param name="success">Whether the song was finished successfully</param>
        private static void ProcessFinish(JToken status, bool success)
        {
            WasCaughtExit = true;
            if (!success)
            {
                lock (MapHistory)
                {
                    MapHistory.Add(new()
                    {
                        NoteCount = 1,
                        Type = HistoryType.LevelFail
                    });
                }
                JustFailed = true;
            }
            SongEnded?.Invoke(typeof(GameStatus), new SongEndedEventArgs(success));
            StateTransition?.Invoke(typeof(GameStatus), new StateTransitionEventArgs(false));
            PreviousMap = CurrentMap;
            CurrentMap = null;
        }

        /// <summary>Makes necessary changes, and fires off events for a player mistake resulting in a broken combo.</summary>
        /// <param name="perf">The status->performance object from the game</param>
        private static void ProcessMistake(JToken perf)
        {
            int RawPosition = perf.Value<int>("songPosition");
            TimeSpan SongPosition = TimeSpan.FromMilliseconds(RawPosition);
            CheckFailure(SongPosition);

            lock (MapHistory)
            {
                if (MapHistory.Count == 0 || MapHistory[^1].Type != HistoryType.Mistake)
                {
                    MapHistory.Add(new()
                    {
                        Type = HistoryType.Mistake,
                        NoteCount = 1,
                        EndTime = SongPosition
                    });
                }
                else
                {
                    MapHistory[^1].NoteCount++;
                    MapHistory[^1].EndTime = SongPosition;
                }
            }

            Mistake?.Invoke(typeof(GameStatus), new MapProgressEventArgs(MapHistory[^1].NoteCount, SongPosition));
        }

        /// <summary>Makes necessary changes, and fires off events for a player cutting a note correctly.</summary>
        /// <param name="perf">The status->performance object from the game</param>
        private static void ProcessCut(JToken perf)
        {
            int RawPosition = perf.Value<int>("songPosition");
            TimeSpan SongPosition = TimeSpan.FromMilliseconds(RawPosition);
            CheckFailure(SongPosition);

            lock (MapHistory)
            {
                if (MapHistory.Count == 0 || MapHistory[^1].Type != HistoryType.Hit)
                {
                    MapHistory.Add(new()
                    {
                        Type = HistoryType.Hit,
                        NoteCount = 1,
                        EndTime = SongPosition
                    });
                }
                else
                {
                    MapHistory[^1].NoteCount++;
                    MapHistory[^1].EndTime = SongPosition;
                }
            }

            Cut?.Invoke(typeof(GameStatus), new MapProgressEventArgs(MapHistory[^1].NoteCount, SongPosition));
        }

        /// <summary>Checks whether we recently failed, and if so, sets the rejoin time to this event.</summary>
        /// <remarks>This is a workaround for the multiplayer mod not setting the song position in time for the SongStart event. Instead we just get the position of the next event after rejoining.</remarks>
        /// <param name="position">The current song position</param>
        private static void CheckFailure(TimeSpan position)
        {
            if (!JustFailed) { return; }
            lock (MapHistory)
            {
                // Amend fail event with rejoin time
                HistoryEntry? FailEntry = MapHistory.FindLast(x => x.Type == HistoryType.LevelFail);
                if (FailEntry != null) { FailEntry.EndTime = position; }

                // Add join event
                MapHistory.Add(new()
                {
                    Type = HistoryType.Join,
                    EndTime = position,
                    NewDifficulty = CurrentMap?.DifficultyPlaying?.Difficulty ?? Difficulty.None,
                    NoteCount = 1
                });
            }
            JustFailed = false;
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
            ParsePlayStats(Result, root["levelStats"]);
            return Result;
        }

        /// <summary>Populates a map info object with play stats from the game.</summary>
        /// <param name="map">The map info object to fill the Stats* fields of</param>
        /// <param name="root">The "levelStats" object from the level event info</param>
        private static void ParsePlayStats(MapInfoPlaying map, JToken? root)
        {
            if (root == null || !root.HasValues) { return; }

            PlayStats? ParseDifficulty(string diff)
            {
                JToken? StatsArray = root?[diff];
                if (StatsArray == null) { return null; }
                return new()
                {
                    ScoreIsValid = StatsArray.Value<bool>("scoreIsValid"),
                    FullCombo = StatsArray.Value<bool>("isFullCombo"),
                    MaxCombo = StatsArray.Value<int>("maxCombo"),
                    HighScore = StatsArray.Value<int>("highScore"),
                    PlayCount = StatsArray.Value<int>("playCount"),
                    MaxRank = StatsArray.Value<string?>("maxRank")
                };
            }

            map.StatsEasy = ParseDifficulty("easy");
            map.StatsNormal = ParseDifficulty("normal");
            map.StatsHard = ParseDifficulty("hard");
            map.StatsExpert = ParseDifficulty("expert");
            map.StatsExpertPlus = ParseDifficulty("expertPlus");
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
            public TimeSpan SongTime { get; set; }
            
            public MapProgressEventArgs(int sequence, TimeSpan time)
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
