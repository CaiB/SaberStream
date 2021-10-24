using Newtonsoft.Json.Linq;
using SaberStream.Targets;
using System;
using System.IO;
using System.Linq;

namespace SaberStream.Data
{
    static class NPSCalc
    {
        /// <summary>Reads the array of note objects out of the given difficulty file</summary>
        /// <param name="path">The (JSON) .dat file for the specific difficulty to read from</param>
        /// <returns>All notes and bombs in the level, or null if something went wrong while reading</returns>
        private static JArray? ReadLevelFile(string path)
        {
            JObject JSON;
            using (StreamReader Reader = File.OpenText(path)) { JSON = JObject.Parse(Reader.ReadToEnd()); }
            JArray? Notes = (JArray?)JSON["_notes"];
            return Notes;
        }

        /// <summary>Reads the Info.dat file for a level and retrieves some basic info from it.</summary>
        /// <param name="folder">The directory where the Info.dat file is located</param>
        /// <param name="difficulty">The difficulty to look for</param>
        /// <param name="type">The level style to look for</param>
        /// <returns>A Tuple of the difficulty-specific .dat filename, and the BPM of the level, or null if something went wrong while reading</returns>
        private static Tuple<string, float>? ReadMainFile(string folder, Difficulty difficulty, string type)
        {
            JObject JSON;
            using (StreamReader Reader = File.OpenText(Path.Combine(folder, "Info.dat"))) { JSON = JObject.Parse(Reader.ReadToEnd()); }
            float BPM = JSON.Value<float>("_beatsPerMinute");
            JToken? Map = JSON["_difficultyBeatmapSets"]?.Where(x => x.Value<string>("_beatmapCharacteristicName") == type)?.First();
            if (Map == null) { return null; }
            string? FileName = Map["_difficultyBeatmaps"]?.Where(x => x.Value<string>("_difficulty") == difficulty.ToString())?.First().Value<string>("_beatmapFilename");
            if (FileName == null) { return null; }
            return new(FileName, BPM);
        }

        /// <summary>Finds how many song beats a single NPS analysis window should occupy</summary>
        /// <param name="secondsPerBlock">The length, in seconds, of each NPS analysis window</param>
        /// <param name="bpm">The BPM of the song</param>
        /// <returns>The length in beats that each NPS analysis window is</returns>
        private static float GetNoteReadInterval(float secondsPerBlock, float bpm) => (secondsPerBlock / 60F) * bpm;

        /// <summary>Takes in the full note array from the level, and analyzes how many notes are in each window. Bombs are ignored.</summary>
        /// <param name="notes">The notes, as read from the level difficulty file</param>
        /// <param name="secondsPerBlock">The length, in seconds, of each NPS analysis window</param>
        /// <param name="songLength">The length of the song, in seconds</param>
        /// <param name="bpm">The BPM of the song</param>
        /// <returns>An array containing the number of notes contained in every secondsPerBlock-long window of the map (not NPS)</returns>
        private static int[] GetBinnedNoteCounts(JArray notes, float secondsPerBlock, float songLength, float bpm)
        {
            int BinCount = (int)Math.Ceiling(songLength / secondsPerBlock);
            int[] Bins = new int[BinCount];
            float BeatInterval = GetNoteReadInterval(secondsPerBlock, bpm);
            for (int i = 0; i < Bins.Length; i++)
            {
                Bins[i] = notes.Where(x => x.Value<float>("_time") > (i * BeatInterval) &&
                                           x.Value<float>("_time") <= ((i + 1) * BeatInterval) &&
                                           (x.Value<int>("_type") == 0 || x.Value<int>("_type") == 1))
                               .Count();
            }
            return Bins;
        }

        /// <summary>Converts a NPS figure into a colour hue for the difficulty mapping</summary>
        /// <param name="notesPerSec">The number of notes per second, as a rough approximation of difficulty</param>
        /// <returns>A hue, between 0 and 360</returns>
        private static float GetHueForSpeed(float notesPerSec)
        {
            if (notesPerSec < 3.0F) { return 120F; } // Green
            if (notesPerSec < 4.5F) { return 120F - (((notesPerSec - 3.0F) / 1.5F) * 60F); } // Green -> Yellow
            if (notesPerSec < 6.0F) { return 60F - (((notesPerSec - 4.5F) / 1.5F) * 60F); } // Yellow -> Red
            if (notesPerSec < 8.0F) { return 360F - (((notesPerSec - 6.0F) / 2.0F) * 80F); } // Red -> Pink
            return 280F; // Pink
        }

        /// <summary>Takes a set of analyzed NPS bins, and converts them into a texture using the colour-difficulty scale</summary>
        /// <param name="bins">The NPS bins to convert. Each entry will be converted to 1 pixel</param>
        /// <param name="secondsPerBlock">How many seconds of notes are contained in each bin, to calculate NPS</param>
        /// <returns>A texture of at least 1 pixel representing the difficulty map</returns>
        private static byte[] BinsToTexture(int[] bins, float secondsPerBlock)
        {
            byte[] Result = new byte[4 * bins.Length];
            for (int i = 0; i < bins.Length; i++)
            {
                float Hue = GetHueForSpeed(bins[i] / secondsPerBlock);
                uint Colour = HsvToRgb(Hue, 0.9, 0.8);
                Result[(i * 4) + 0] = (byte)(Colour >> 16);
                Result[(i * 4) + 1] = (byte)(Colour >> 8);
                Result[(i * 4) + 2] = (byte)Colour;
                Result[(i * 4) + 3] = 255;
            }
            return Result;
        }

        /// <summary>Reads a level and computes a coloured difficulty texture for the specified difficulty. Passes it to <see cref="Overlay"/> when done for display.</summary>
        /// <remarks>This does some file I/O, bulk parsing, processing, and texture generation, and as such may take some time to execute. Don't run this on a latency-sensitive thread.</remarks>
        /// <param name="mapFolder">The folder where the level is contained</param>
        /// <param name="difficulty">The difficulty to analyze</param>
        /// <param name="type">The level type to analyze</param>
        /// <param name="songLength">How long the song is</param>
        /// <param name="secondsPerBlock">The length, in seconds, that each output pixel should correspond to</param>
        public static void CalculateDifficultyMap(string mapFolder, Difficulty difficulty, string type, float songLength, float secondsPerBlock)
        {
            if (!File.Exists(Path.Combine(mapFolder, "info.dat")))
            {
                Console.WriteLine($"Could not find map info for {mapFolder} {type}:{difficulty}");
                Overlay.NewDifficultyTexture(new byte[] { 0, 0, 0, 0 });
                return;
            }

            Tuple<string, float>? LevelInfo = ReadMainFile(mapFolder, difficulty, type);
            if (LevelInfo == null)
            {
                Console.WriteLine($"Could not read map info for {mapFolder} {type}:{difficulty}");
                Overlay.NewDifficultyTexture(new byte[] { 0, 0, 0, 0 });
                return;
            }

            JArray? Notes = ReadLevelFile(Path.Combine(mapFolder, LevelInfo.Item1));
            if (Notes == null)
            {
                Console.WriteLine($"Could not read notes from {LevelInfo.Item1} ({type}:{difficulty})");
                Overlay.NewDifficultyTexture(new byte[] { 0, 0, 0, 0 });
                return;
            }

            int[] Bins = GetBinnedNoteCounts(Notes, secondsPerBlock, songLength, LevelInfo.Item2);
            byte[] Texture = BinsToTexture(Bins, secondsPerBlock);
            Overlay.NewDifficultyTexture(Texture);
        }

        /// <summary>Converts a HSV value to RGB.</summary>
        /// <param name="h">Hue</param>
        /// <param name="S">Saturation</param>
        /// <param name="V">Value</param>
        /// <returns>RGB, 8 bits each, in format 0x0RGB</returns>
        public static uint HsvToRgb(double h, double S, double V) // TODO: Copy-pasted, this looks like it could use some optimization.
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {
                    // Red is the dominant color
                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color
                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color
                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color
                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.
                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.
                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            byte r = (byte)Clamp((int)(R * 255.0));
            byte g = (byte)Clamp((int)(G * 255.0));
            byte b = (byte)Clamp((int)(B * 255.0));
            return (uint)((r << 16) | (g << 8) | b);
        }

        /// <summary>Clamps a value to 0-255</summary>
        public static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

    }
}
