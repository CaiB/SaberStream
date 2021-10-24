using System;

namespace SaberStream.Data
{
    public record MapInfo
    {
        /// <summary>The BeatSaver key for this map, if it is identifiable and valid. Some maps may be null, including built-in maps.</summary>
        public string? Key { get; set; }
        public string? SongName { get; init; }
        public string? SongSubName { get; init; }
        public string? SongAuthor { get; init; }
        public string? MapAuthor { get; init; }
        public TimeSpan Length { get; init; }

        public DifficultyInfo? Easy { get; init; } // TODO: Add support for non-Standard type maps (90, one-handed, 360, etc)
        public DifficultyInfo? Normal { get; init; }
        public DifficultyInfo? Hard { get; init; }
        public DifficultyInfo? Expert { get; init; }
        public DifficultyInfo? ExpertPlus { get; init; }

        public MapInfo(string? key) { this.Key = key; }

        public DifficultyInfo? GetDifficulty(Difficulty diff)
        {
            if (diff.HasFlag(Difficulty.Easy)) { return this.Easy; }
            if (diff.HasFlag(Difficulty.Normal)) { return this.Normal; }
            if (diff.HasFlag(Difficulty.Hard)) { return this.Hard; }
            if (diff.HasFlag(Difficulty.Expert)) { return this.Expert; }
            if (diff.HasFlag(Difficulty.ExpertPlus)) { return this.ExpertPlus; }
            return null;
        }
    }

    public record MapInfoBeatSaver : MapInfo
    {
        public DateTime Uploaded { get; init; }
        public string? DownloadURL { get; init; }

        public int Upvotes { get; init; }
        public int Downvotes { get; init; }
        public int TotalVotes { get => this.Upvotes + this.Downvotes; }
        public float ApprovalRating { get => (float)this.Upvotes / this.TotalVotes; }
        public int DownloadCount { get; init; }

        public MapInfoBeatSaver(string key) : base(key) { }
    }

    public record MapInfoPlaying : MapInfo
    {
        /// <summary>Raw data representing a PNG image of the cover art.</summary>
        public byte[]? CoverArt { get; init; }

        /// <summary>A hash that uniquely identifies a specific version of a specific map.</summary>
        public string? Hash { get; init; }

        /// <summary>The directory that this map is located in. Info.dat will be located directly in this folder.</summary>
        public string? MapFolder { get; init; }

        public float BPM { get; init; }
        public float NJS { get; init; }
        public TimeSpan SongPosition { get; init; }
        public DifficultyInfo? DifficultyPlaying { get; init; }

        public MapInfoPlaying(string? key) : base(key) { }
    }
}
