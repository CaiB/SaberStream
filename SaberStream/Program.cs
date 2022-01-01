using Newtonsoft.Json.Linq;
using SaberStream.Sources;
using SaberStream.Targets;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SaberStream
{
    class Program
    {
        private const string CONFIG_FILE = "SaberStream-Config.json";

        [STAThread]
        public static void Main(string[] args)
        {
            // Read config file
            if (!File.Exists(CONFIG_FILE)) { throw new FileNotFoundException($"Config file was not found, make sure it exists as '{CONFIG_FILE}'."); }
            JObject JSON;
            using (StreamReader Reader = File.OpenText(CONFIG_FILE)) { JSON = JObject.Parse(Reader.ReadToEnd()); }

            // Read general config
            JToken ModulesConfig = JSON["Modules"] ?? throw new Exception("Could not find 'Modules' section in config file.");
            string TempDir = JSON.Value<string>("TempDir") ?? throw new Exception("Could not find 'TempDir' in config file.");
            string GameDir = JSON.Value<string>("GameDir") ?? throw new Exception("Could not find 'GameDir' in config file.");
            string? GameSocket = JSON.Value<string>("GameSocket");

            // Basic operations
            ConsoleCommand.Start();
            Game.Start(TempDir, GameDir);
            ConsoleCommand.DownloadRequest += Game.HandleDownloadRequest;
            GameStatus.Start(GameSocket);
            SongLogFile.Start();

            bool TwitchEnabled = ModulesConfig.Value<bool?>("Twitch") ?? true;
            if (TwitchEnabled)
            {
                // Read Twitch config
                JToken TwitchConfig = JSON["Twitch"] ?? throw new Exception("Could not find 'Twitch' section in config file.");
                string TwitchChannel = TwitchConfig.Value<string>("ChannelName") ?? throw new Exception("Could not find 'Twitch'->'ChannelName' in config file.");
                string TwitchUser = TwitchConfig.Value<string>("UserName") ?? throw new Exception("Could not find 'Twitch'->'UserName' in config file.");
                string TwitchToken = TwitchConfig.Value<string>("AuthToken") ?? throw new Exception("Could not find 'Twitch'->'AuthToken' in config file.");

                // Start Twitch integration
                Twitch.Connect(TwitchUser, TwitchToken, TwitchChannel);
                TwitchResponder TwitchResp = new();
            }

            // Read overlay config
            JToken OverlayConfig = JSON["Overlay"] ?? throw new Exception("Could not find 'Overlay' section in the config file.");

            // Start overlay
            Overlay Overlay;
            Thread WindowThread = new(() =>
            {
                Overlay = new(OverlayConfig);
                Overlay.Run();
            });
            WindowThread.Name = "Overlay Window";
            WindowThread.Start();

            ApplicationConfiguration.Initialize();
            Application.Run(new QueueViewer());
        }
    }
}
