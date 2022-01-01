using System;

namespace SaberStream.Sources
{
    public static class CommonEvents
    {
        public delegate void ExitHandler(object? sender, EventArgs evt);
        public static event ExitHandler? Exit;
        public static void InvokeExit(object? sender, EventArgs evt)
        {
            Console.WriteLine("Exiting...");
            Exit?.Invoke(sender, evt);
        }

        public delegate void DownloadRequestHandler(object? sender, DownloadRequestEventArgs evt);

        /// <summary>Invoked whenever the player approves/requests a song download.</summary>
        public static event DownloadRequestHandler? DownloadRequest;

        public static void InvokeDownloadRequest(object? sender, DownloadRequestEventArgs evt) => DownloadRequest?.Invoke(sender, evt);
    }

    public class DownloadRequestEventArgs : EventArgs
    {
        public string Key { get; set; }
        public DownloadRequestEventArgs(string key) { this.Key = key; }
    }
}
