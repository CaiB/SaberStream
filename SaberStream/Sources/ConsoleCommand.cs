using SaberStream.Targets;
using System;
using System.Threading;

namespace SaberStream.Sources
{
    public static class ConsoleCommand
    {
        private static Thread? ProcessThread;
        private static bool Continue = true;

        /// <summary>Begins listening for, and handling console commands.</summary>
        public static void Start()
        {
            ProcessThread = new Thread(ProcessCommands);
            ProcessThread.Start();
            CommonEvents.Exit += HandleExit;
        }

        /// <summary>Actually processes the commands in a loop until <see cref="Continue"/> becomes false.</summary>
        /// <remarks>This method blocks, so should be run on a thread.</remarks>
        private static void ProcessCommands()
        {
            while (Continue)
            {
                string? Line = Console.ReadLine();
                if (Line == null) { continue; }

                int FirstSpaceIndex = Line.IndexOf(' ');
                string Command = (FirstSpaceIndex < 0) ? Line : Line.Substring(0, FirstSpaceIndex);
                string? Remainder = (FirstSpaceIndex > 0 && Line.Length > FirstSpaceIndex + 1) ? Line.Substring(FirstSpaceIndex + 1) : null;
                Command = Command.ToLower();

                if (Command == "exit" || Command == "quit" || Command == "stop") { CommonEvents.InvokeExit(null, new EventArgs()); }
                else if (Command == "help") { PrintHelp(); }
                else if (Command == "dl" && Remainder != null) { DownloadRequest?.Invoke(null, new DownloadRequestEventArgs(Remainder.ToLower())); }
                else if (Command == "r") { Game.ReloadLibrary(); }
                else if (Command == "msg" && Remainder != null) { Twitch.SendMessage(Remainder); }
                else { Console.WriteLine("Unrecognized command. Run 'help' to see usage info."); }
            }
        }

        /// <summary>Shows information about available console commands.</summary>
        private static void PrintHelp()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  exit|quit|stop: Close connections and exit");
            Console.WriteLine("  dl <key>: Requests the map with the given key be downloaded from BeatSaver");
            Console.WriteLine("  r: Reload the game's song library");
        }

        private static void HandleExit(object? sender, EventArgs evt) => Continue = false;

        public class DownloadRequestEventArgs : EventArgs
        {
            public string Key { get; set; }
            public DownloadRequestEventArgs(string key) { this.Key = key; }
        }
        public delegate void DownloadRequestHandler(object? sender, DownloadRequestEventArgs evt);

        /// <summary>Invoked whenever the player approves/requests a song download.</summary>
        public static event DownloadRequestHandler? DownloadRequest;
    }
}
