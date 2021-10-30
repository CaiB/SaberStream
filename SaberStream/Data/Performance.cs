using System;

namespace SaberStream.Data
{
    /// <summary>A representation of how the player is currently doing.</summary>
    public record Performance
    {
        public int Score { get; init; }

        public string? Rank { get; init; }

        /// <summary>How many notes into the song the player is, hit or miss.</summary>
        public int NotesPassed { get; init; }

        public int NotesHit { get; init; }

        public int Combo { get; init; }

        public int MaxCombo { get; init; }
    }

    public record PlayStats
    {
        public bool ScoreIsValid { get; init; }
        public int HighScore { get; init; }
        public int MaxCombo { get; init; }
        public bool FullCombo { get; init; }
        public int PlayCount { get; init; }
        public string? MaxRank { get; init; }
    }
}
