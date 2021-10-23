using SaberStream.Data;
using SaberStream.Helpers;
using SaberStream.Sources;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Threading;

namespace SaberStream.Targets
{
    public static class Game
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        private const string GAME_PROCESS = "Beat Saber";
        private const string GAME_SUBDIR = "Beat Saber_Data\\CustomLevels\\";
        private static string? TempDirectory, MapDirectory;

        /// <summary>Prepares the game interface to be used.</summary>
        /// <param name="tempDir">A directory with write access where maps can be temporarily stored while downloading</param>
        /// <param name="gameDir">The directory where the game is installed, should end in "Beat Saber\"</param>
        public static void Start(string tempDir, string gameDir)
        {
            TempDirectory = tempDir;
            MapDirectory = Path.Combine(gameDir, GAME_SUBDIR);
            // TODO: Re-enable this
            //if (!Directory.Exists(MapDirectory)) { throw new DirectoryNotFoundException($"Could not locate the game's map folder at '{MapDirectory}'."); }
        }

        /// <summary>Sends Ctrl+R keyboard input to the game to refresh the song library.</summary>
        public static void ReloadLibrary()
        {
            Process? Game = Process.GetProcessesByName(GAME_PROCESS).FirstOrDefault();
            if (Game == null) { return; }
            IntPtr WindowHandle = Game.MainWindowHandle;
            IntPtr PreviousWindow = GetForegroundWindow();

            SetForegroundWindow(WindowHandle);
            SendMessage(WindowHandle, 0x0100, 0x11, 0x00000000); // CTRL down
            Thread.Sleep(10);
            SendMessage(WindowHandle, 0x0100, 0x52, 0x00000000); // R down
            Thread.Sleep(10);
            SendMessage(WindowHandle, 0x0101, 0x52, 0x00010003); // R up
            Thread.Sleep(10);
            SendMessage(WindowHandle, 0x0101, 0x11, 0x00010003); // CTRL up

            SetForegroundWindow(PreviousWindow);
            Console.WriteLine("Game song library reloaded");
        }

        /// <summary>Downloads a map from Beat Saver, installs it in the game's folder, and reloads the library.</summary>
        /// <param name="key">The map key to download</param>
        private static void DownloadSong(string key)
        {
            if (MapDirectory == null || TempDirectory == null) { throw new InvalidOperationException("Must set directories before attempting a download"); }
            MapInfoBeatSaver? Map = BeatSaver.GetMapInfo(key);
            if (Map == null) { Console.WriteLine($"Couldn't get info about map '{key}', download failed."); return; }
            if (Map.DownloadURL == null) { Console.WriteLine($"Couldn't get download URL for map '{key}', download failed."); return; }

            try
            {
                string? FileName;
                string ZIPFilePath;
                using (WebClient Client = new())
                {
                    Client.OpenRead(Map.DownloadURL);
                    string? DispositionHeader = Client.ResponseHeaders?["content-disposition"];
                    FileName = (DispositionHeader == null) ? null : new ContentDisposition(DispositionHeader).FileName;
                    ZIPFilePath = Path.Combine(TempDirectory, FileName ?? $"{key}.zip");
                    Client.DownloadFile(Map.DownloadURL, ZIPFilePath);
                }
                string TargetFolder = Path.Combine(MapDirectory, FileName ?? key);
                if (Directory.Exists(TargetFolder)) { Console.WriteLine($"The map '{key}' has already been downloaded before!"); return; }

                Directory.CreateDirectory(TargetFolder);
                ZipFile.ExtractToDirectory(ZIPFilePath, TargetFolder);
                ReloadLibrary();
                Console.WriteLine($"Map '{key}' downloaded and installed successfully.");
            }
            catch(Exception exc)
            {
                Console.WriteLine($"Failed to download map '{key}':");
                Console.WriteLine(exc);
            }
        }

        public static void HandleDownloadRequest(object? sender, ConsoleCommand.DownloadRequestEventArgs evt) => DownloadSong(evt.Key);
    }
}
