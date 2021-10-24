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

    /// <summary>A history entry repesenting a series of contiguous note hits or mistakes.</summary>
    public class PerformanceEntry
    {
        public int NoteCount { get; set; }
        public bool WasCorrect { get; set; }
        public float LastActionTime { get; set; }
    }
}
