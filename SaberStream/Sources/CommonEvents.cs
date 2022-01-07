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
            Environment.Exit(0);
        }

        /// <summary>Invoked whenever the player approves/requests a song download.</summary>
        public static event EventHandler<DownloadRequestEventArgs>? DownloadRequest;
        public static void InvokeDownloadRequest(object? sender, DownloadRequestEventArgs evt) => DownloadRequest?.Invoke(sender, evt);

        /// <summary>Invoked whenever the player requests a song deletion.</summary>
        public static event EventHandler<DeleteRequestEventArgs>? DeleteRequest;
        public static void InvokeDeleteRequest(object? sender, DeleteRequestEventArgs evt) => DeleteRequest?.Invoke(sender, evt);
    }

    public class DeleteRequestEventArgs : EventArgs
    {
        public string Path { get; set; }
        public DeleteRequestEventArgs(string path) { this.Path = path; }
    }

    public class DownloadRequestEventArgs : EventArgs
    {
        public string Key { get; set; }
        public DownloadRequestEventArgs(string key) { this.Key = key; }
    }
}
