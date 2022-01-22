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
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

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
            if (!Directory.Exists(MapDirectory)) { throw new DirectoryNotFoundException($"Could not locate the game's map folder at '{MapDirectory}'."); }
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
                string? FileName = null;
                string ZIPFilePath;
                using (WebClient Client = new())
                {
                    Client.OpenRead(Map.DownloadURL);

                    // Try to get the intended file name
                    string? DispositionHeader = Client.ResponseHeaders?["content-disposition"];
                    if (DispositionHeader != null) { FileName = GetFileNameFromHeader(DispositionHeader); }
                    // This doesn't work because of illegal characters in the content-disposition header, thanks BeatSaver
                    // FileName = (DispositionHeader == null) ? null : new ContentDisposition(DispositionHeader).FileName;

                    // Make sure there's no characters disallowed by the filesystem
                    if (FileName != null) { FileName = Regex.Replace(FileName, @"<|>|:|""|\/|\\|\||\?|\*|[\x00-\x1F]", "_", RegexOptions.CultureInvariant); }

                    // Download the file, falling back on a name containing just the key in the worst case
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

        /// <summary>Tries to parse the filename from a content-disposition header using various methods</summary>
        /// <remarks>It looks like the Beat Saver servers return invalid headers, so this is an ugly hack to work around that.</remarks>
        /// <param name="contentDisposition">The value of the content-disposition header</param>
        /// <returns>The filename, if it was able to be parsed, or null otherwise</returns>
        private static string? GetFileNameFromHeader(string contentDisposition)
        {
            if (string.IsNullOrWhiteSpace(contentDisposition)) { return null; }
            Regex UTF8Matcher = new(@"filename\*=UTF-8''([\w%\-\.]+)(?:; ?|$)", RegexOptions.IgnoreCase);
            Regex ASCIIMatcher = new(@"filename=([""']?)(.*?[^\\])\1(?:; ?|$)", RegexOptions.IgnoreCase);

            Match UTF8Match = UTF8Matcher.Match(contentDisposition);
            if (UTF8Match.Success && UTF8Match.Groups.Count > 2)
            {
                string? DecodeResult = HttpUtility.UrlDecode(UTF8Match.Groups[2].Value);
                if (!string.IsNullOrWhiteSpace(DecodeResult)) { return DecodeResult; }
            }

            Match ASCIIMatch = ASCIIMatcher.Match(contentDisposition);
            if (ASCIIMatch.Success && ASCIIMatch.Groups.Count > 2)
            {
                string? DecodeResult = HttpUtility.UrlDecode(ASCIIMatch.Groups[2].Value);
                if (!string.IsNullOrWhiteSpace(DecodeResult)) { return DecodeResult; }
                
            }

            throw new InvalidDataException($"The content-disposition header could not be parsed. Header was \"{contentDisposition}\"");
        }

        private static void DeleteSong(string path)
        {
            if (MapDirectory == null) { return; }
            Uri MapDir = new(MapDirectory);
            if (!MapDir.IsBaseOf(new(path))) { Console.WriteLine($"Not deleting \"{path}\" because it wasn't in \"{MapDirectory}\""); return; }
            if (!Directory.Exists(path)) { Console.WriteLine($"Not deleting \"{path}\" because it couldn't be found."); return; }
            try
            {
                Directory.Delete(path, true);
                ReloadLibrary();
                Console.WriteLine($"Deleted {path} successfully.");
            }
            catch (Exception exc) { Console.WriteLine($"Failed to delete \"{path}\": {exc}"); }
        }

        public static void HandleDownloadRequest(object? sender, DownloadRequestEventArgs evt) => DownloadSong(evt.Key);

        public static void HandleDeleteRequest(object? sender, DeleteRequestEventArgs evt) => DeleteSong(evt.Path);
    }
}
