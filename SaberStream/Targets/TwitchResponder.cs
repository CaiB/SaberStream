using SaberStream.Data;
using SaberStream.Helpers;
using SaberStream.Sources;
using System;
using System.Text;

namespace SaberStream.Targets
{
    public class TwitchResponder
    {
        public TwitchResponder()
        {
            Twitch.MessageReceived += MessageHandler;
        }

        /// <summary>Handles messages in chat, and responds to requests.</summary>
        private void MessageHandler(object? sender, Twitch.MessageReceivedEventArgs evt)
        {
            string Message = evt.Message.Message;
            if (Message.StartsWith("!bsr"))
            {
                int SpaceIndex = Message.IndexOf(' ');
                if (SpaceIndex < 0) { Twitch.SendMessage("Requests are welcome! Please specify a map key."); return; }
                string Key = Message.Substring(SpaceIndex + 1).ToLower();
                if (Key.Length == 0 || Key.Length >= 7) { Twitch.SendMessage("That doesn't appear to be a valid map key, it should be a few letters/numbers long."); return; }
                
                MapInfoBeatSaver? Map = BeatSaver.GetMapInfo(Key);
                if (Map == null) { Twitch.SendMessage("Couldn't get info about that level :("); return; }

                string Info = $"\"{Map.SongName}\" by \"{Map.SongAuthor}\", mapped by {Map.MapAuthor}: {Map.DownloadCount} DLs, {(Map.ApprovalRating * 100F):F0}% approval, {Map.Length.Minutes}m{Map.Length.Seconds}s.";
                float SongLength = (float)Map.Length.TotalSeconds;
                const string DIFF_START = "Difficulties: ";

                StringBuilder Diffs = new(DIFF_START);
                if (Map.Easy != null) { Diffs.Append(FormatNS(Map.Easy, SongLength)); }
                if (Map.Normal != null) { Diffs.Append(FormatNS(Map.Normal, SongLength)); }
                if (Map.Hard != null) { Diffs.Append(FormatNS(Map.Hard, SongLength)); }
                if (Map.Expert != null) { Diffs.Append(FormatNS(Map.Expert, SongLength)); }
                if (Map.ExpertPlus != null) { Diffs.Append(FormatNS(Map.ExpertPlus, SongLength)); }

                string DiffDesc;
                if (Diffs.ToString() == DIFF_START) { DiffDesc = "No regular difficulties found. Maybe 306/one-handed-only?"; }
                else { DiffDesc = Diffs.Remove(Diffs.Length - 2, 2).ToString(); } // Remove the trailing ', '

                Twitch.SendMessage(Info + '\n' + DiffDesc);
            }
        }

        /// <summary>Formats a specific difficulty's note speed into a concise format for chat.</summary>
        /// <param name="diff">The difficulty to output</param>
        /// <param name="songLength">How long the song is, in seconds</param>
        /// <returns>A string formatted like "H=3.67, " where H is the difficulty, and 3.67 is the average NPS of that difficulty</returns>
        private static string FormatNS(DifficultyInfo diff, float songLength) => $"{DifficultyUtil.GetShortName(diff.Difficulty)}={(diff.NoteCount / songLength):F2}, ";
    }
}
