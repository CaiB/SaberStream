using SaberStream.Sources;
using System;
using System.IO;

namespace SaberStream.Targets
{
    /// <summary>Listens for song started events, and writes each one into a log file to be used as YouTube chapter markers in the VOD.</summary>
    public static class SongLogFile
    {
        private static StreamWriter? LogFile;

        public static void Start()
        {
            LogFile = new(File.OpenWrite("BeatSaberLog-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt"));
            GameStatus.SongStarted += HandleSongStart;
        }

        private static void HandleSongStart(object? sender, GameStatus.SongStartedEventArgs evt)
        {
            if (evt.Retry) { return; }
            LogFile?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ' ' + evt.Beatmap.SongName);
            LogFile?.Flush();
        }
    }
}
