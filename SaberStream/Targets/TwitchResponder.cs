﻿using SaberStream.Data;
using SaberStream.Helpers;
using SaberStream.Sources;
using System;
using System.Text;

namespace SaberStream.Targets
{
    /// <summary>Responds to !bsr requests in Twitch chat, and responds with basic map information from BeatSaver.</summary>
    public class TwitchResponder
    {
        public TwitchResponder()
        {
            Twitch.MessageReceived += HandleChatMessage;
            GameStatus.SongStarted += HandleSongStart;
        }

        /// <summary>Handles messages in chat, and responds to requests.</summary>
        private void HandleChatMessage(object? sender, Twitch.MessageReceivedEventArgs evt)
        {
            string Message = evt.Message.Message;
            if (Message.StartsWith("!bsr", StringComparison.CurrentCultureIgnoreCase))
            {
                int SpaceIndex = Message.IndexOf(' ');
                if (SpaceIndex < 0)
                {
                    Twitch.SendMessage("Requests are welcome! Please specify a map key.");
                    Twitch.SendMessage("To find a song, please go to https://beatsaver.com, search for the song you want, and then paste the key here in chat.");
                    Twitch.SendMessage("We prefer to play music, not meme maps. A rating above 80% usually means the map is fun.");
                    return;
                }
                string Key = Message.Substring(SpaceIndex + 1).ToLower();
                if (Key.Contains(' ')) { Key = Key.Substring(0, Key.IndexOf(' ')); }
                if (Key.Length == 0 || Key.Length >= 7) { Twitch.SendMessage("That doesn't appear to be a valid map key, it should be a few letters/numbers long."); return; }
                
                MapInfoBeatSaver? Map = BeatSaver.GetMapInfo(Key);
                if (Map == null) { Twitch.SendMessage("Couldn't get info about that level :("); return; }

                string Info = $"\"{Map.SongName}\" by \"{Map.SongAuthor}\", mapped by {Map.MapAuthor}: {Map.TotalVotes} votes, {(Map.ApprovalRating * 100F):F0}% approval, {Map.Length.Minutes}m{Map.Length.Seconds}s.";
                float SongLength = (float)Map.Length.TotalSeconds;
                const string DIFF_START = "Difficulties: ";

                StringBuilder Diffs = new(DIFF_START);
                if (Map.Easy != null) { Diffs.Append(FormatNS(Map.Easy, SongLength)); }
                if (Map.Normal != null) { Diffs.Append(FormatNS(Map.Normal, SongLength)); }
                if (Map.Hard != null) { Diffs.Append(FormatNS(Map.Hard, SongLength)); }
                if (Map.Expert != null) { Diffs.Append(FormatNS(Map.Expert, SongLength)); }
                if (Map.ExpertPlus != null) { Diffs.Append(FormatNS(Map.ExpertPlus, SongLength)); }

                string DiffDesc;
                if (Diffs.ToString() == DIFF_START) { DiffDesc = "No regular difficulties found. Maybe 360/one-handed-only?"; }
                else { DiffDesc = Diffs.Remove(Diffs.Length - 2, 2).ToString(); } // Remove the trailing ', '

                Console.WriteLine($"Song request from {evt.Message.Username}: key {Key} ({Map.SongName} - {Map.SongAuthor}, Mapped by {Map.MapAuthor})");
                Twitch.SendMessage(Info + '\n' + DiffDesc);

                MapInfoRequest MapRequest = new(Map, Key) { Requestor = evt.Message.Username };
                RequestQueue.AddItem(MapRequest);
            }
            else if (Message.StartsWith("!info", StringComparison.CurrentCultureIgnoreCase) || Message.StartsWith("!key", StringComparison.CurrentCultureIgnoreCase))
            {
                if (GameStatus.CurrentMap == null && GameStatus.PreviousMap != null)
                {
                    MapInfoPlaying Map = GameStatus.PreviousMap;
                    Twitch.SendMessage($"We just played \"{Map.SongName}\" by \"{Map.SongAuthor}\", mapped by {Map.MapAuthor}, key {Map.Key}.");
                }
                else if (GameStatus.CurrentMap != null)
                {
                    MapInfoPlaying Map = GameStatus.CurrentMap;
                    Twitch.SendMessage($"We're currently playing \"{Map.SongName}\" by \"{Map.SongAuthor}\", mapped by {Map.MapAuthor}, key {Map.Key}.");
                }
            }
            else if (Message.StartsWith("!link", StringComparison.CurrentCultureIgnoreCase))
            {
                if (GameStatus.CurrentMap == null && GameStatus.PreviousMap != null) { Twitch.SendMessage($"We just played https://beatsaver.com/maps/{GameStatus.PreviousMap.Key}"); }
                else if (GameStatus.CurrentMap != null) { Twitch.SendMessage($"We're currently playing https://beatsaver.com/maps/{GameStatus.CurrentMap.Key}"); }
            }
            else if (string.Equals(Message, "F", StringComparison.CurrentCultureIgnoreCase))
            {
                Twitch.SendMessage("F");
            }
            else if (Message.StartsWith("!queue", StringComparison.CurrentCultureIgnoreCase) || Message.StartsWith("!list", StringComparison.CurrentCultureIgnoreCase))
            {
                int QueueLength = RequestQueue.GetItemCount();
                if (QueueLength > 0)
                {
                    Twitch.SendMessage(string.Format("There {2} {0} song{1} in the queue. Up next:", QueueLength, QueueLength == 1 ? "" : "s", QueueLength == 1 ? "is" : "are"));
                    for (int i = 0; i < Math.Min(QueueLength, 3); i++)
                    {
                        MapInfo ItemInPos = RequestQueue.GetItem(i);
                        MapInfoRequest? RequestItem = ItemInPos as MapInfoRequest;
                        Twitch.SendMessage($"{ItemInPos.SongName} - {ItemInPos.MapAuthor} ({ItemInPos.Key}) {(RequestItem is not null ? "[" + RequestItem.Requestor + "]" : "")}");
                    }
                }
                else { Twitch.SendMessage("The request queue is currently empty."); }
            }
        }

        /// <summary>Formats a specific difficulty's note speed into a concise format for chat.</summary>
        /// <param name="diff">The difficulty to output</param>
        /// <param name="songLength">How long the song is, in seconds</param>
        /// <returns>A string formatted like "H=3.67, " where H is the difficulty, and 3.67 is the average NPS of that difficulty</returns>
        private static string FormatNS(DifficultyInfo diff, float songLength) => $"{DifficultyUtil.GetShortName(diff.Difficulty)}={(diff.NoteCount / songLength):F2}, ";

        /// <summary>Outputs info to chat about the currently played map when we start playing it.</summary>
        protected static void HandleSongStart(object? sender, GameStatus.SongStartedEventArgs evt)
        {
            if (!evt.Retry) { Twitch.SendMessage($"We are now playing \"{evt.Beatmap.SongName}\" by \"{evt.Beatmap.SongAuthor}\", mapped by \"{evt.Beatmap.MapAuthor}\"."); }
        }
    }
}
