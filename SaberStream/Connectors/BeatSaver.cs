using Newtonsoft.Json.Linq;
using SaberStream.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SaberStream.Helpers
{
    public static class BeatSaver
    {
        private const string BASE_URL = "https://api.beatsaver.com/";
        private const string DETAIL_URL = "maps/id/";
        private const string HASH_URL = "maps/hash/";
        private const int API_TIMEOUT = 5000; // ms

        private static readonly HttpClient HTTP = new();

        /// <summary>Populates a <see cref="MapInfoBeatSaver"/> object with info from the BeatSaver API.</summary>
        /// <param name="key">The map key to get info for</param>
        /// <returns>Data about the given song, or null if something went wrong</returns>
        public static MapInfoBeatSaver? GetMapInfo(string key)
        {
            try
            {
                HTTP.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Task<HttpResponseMessage> ResTask = HTTP.GetAsync(BASE_URL + DETAIL_URL + key);
                if (!ResTask.Wait(API_TIMEOUT))
                {
                    Console.WriteLine("BeatSaver API timed out.");
                    return null;
                }
                HttpResponseMessage Response = ResTask.Result;
                if (!Response.IsSuccessStatusCode)
                {
                    Console.WriteLine("BeatSaver API Error: {0} ({1})", (int)Response.StatusCode, Response.ReasonPhrase);
                    return null;
                }

                // We got a response
                JObject JSON = JObject.Parse(new StreamReader(Response.Content.ReadAsStream()).ReadToEnd());
                JToken? Metadata = JSON["metadata"];
                JToken? Stats = JSON["stats"];
                JToken? NewestVersion = ((JArray?)JSON["versions"])?.First; // TODO: Check if the newest is actually at the top

                if (Metadata == null || Stats == null || NewestVersion == null)
                {
                    Console.WriteLine($"Got response from BeatSaver with missing data for key '{key}'.");
                    return null;
                }

                IEnumerable<JToken>? Difficulties = NewestVersion["diffs"]?.Where(x => x.Value<string>("characteristic") == "Standard");
                if (Difficulties == null)
                {
                    Console.WriteLine($"Could not parse BeatSaver difficulties for key '{key}'.");
                    return null;
                }

                DifficultyInfo? Easy = null, Normal = null, Hard = null, Expert = null, ExpertPlus = null;
                foreach (JToken Diff in Difficulties)
                {
                    string? DifficultyName = Diff.Value<string>("difficulty");
                    int Notes = Diff.Value<int>("notes");
                    int Bombs = Diff.Value<int>("bombs");
                    int Walls = Diff.Value<int>("obstacleCount");

                    switch (DifficultyName)
                    {
                        case "Easy":
                            Easy = new(Difficulty.Easy) { NoteCount = Notes, BombCount = Bombs, WallCount = Walls };
                            break;
                        case "Normal":
                            Normal = new(Difficulty.Normal) { NoteCount = Notes, BombCount = Bombs, WallCount = Walls };
                            break;
                        case "Hard":
                            Hard = new(Difficulty.Hard) { NoteCount = Notes, BombCount = Bombs, WallCount = Walls };
                            break;
                        case "Expert":
                            Expert = new(Difficulty.Expert) { NoteCount = Notes, BombCount = Bombs, WallCount = Walls };
                            break;
                        case "ExpertPlus":
                            ExpertPlus = new(Difficulty.ExpertPlus) { NoteCount = Notes, BombCount = Bombs, WallCount = Walls };
                            break;
                        default:
                            Console.WriteLine($"BeatSaver returned unknown difficulty '{DifficultyName}'.");
                            break;
                    }
                }
                    
                MapInfoBeatSaver Map = new(key)
                {
                    SongName = Metadata.Value<string>("songName") ?? JSON.Value<string>("name"),
                    SongSubName = Metadata.Value<string>("songSubName"),
                    SongAuthor = Metadata.Value<string>("songAuthorName"),
                    MapAuthor = Metadata.Value<string>("levelAuthorName"),
                    Length = TimeSpan.FromSeconds(Metadata.Value<int>("duration")), // TODO: Is this sufficient? If 0, was interpreting difficulty length as well before
                    Uploaded = DateTime.Parse(JSON.Value<string>("uploaded") ?? DateTime.Now.ToString()), // TODO: make this nicer
                    DownloadURL = NewestVersion.Value<string>("downloadURL"),
                    Upvotes = Stats.Value<int>("upvotes"),
                    Downvotes = Stats.Value<int>("downvotes"),
                    DownloadCount = Stats.Value<int>("downloads"),

                    Easy = Easy,
                    Normal = Normal,
                    Hard = Hard,
                    Expert = Expert,
                    ExpertPlus = ExpertPlus
                };
                return Map;
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Failed to get BeatSaver map info for key '{key}':");
                Console.WriteLine(exc);
            }
            return null;
        }

        /// <summary>Gets the map key from a specific map hash from the BeatSaver API.</summary>
        /// <param name="hash">The map's hash that uniquely identifies the version of a map</param>
        /// <returns>The map key, if the request succeeds, otherwise null</returns>
        public static string? GetKeyFromHash(string hash)
        {
            try
            {
                HTTP.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Task<HttpResponseMessage> ResTask = HTTP.GetAsync(BASE_URL + HASH_URL + hash);
                if (!ResTask.Wait(API_TIMEOUT))
                {
                    Console.WriteLine("BeatSaver API timed out.");
                    return null;
                }
                HttpResponseMessage Response = ResTask.Result;
                if (Response.IsSuccessStatusCode)
                {
                    JObject JSON = JObject.Parse(new StreamReader(Response.Content.ReadAsStream()).ReadToEnd());
                    return JSON.Value<string>("id");
                }
                else { Console.WriteLine("BeatSaver API Error: {0} ({1})", (int)Response.StatusCode, Response.ReasonPhrase); }
            }
            catch(Exception exc)
            {
                Console.WriteLine($"Failed to get map key for hash {hash}");
                Console.WriteLine(exc.ToString());
            }
            return null;
        }
    }
}
