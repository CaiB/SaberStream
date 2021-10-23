using System;

namespace SaberStream.Data
{
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

    public class PerformanceEntry
    {
        public int NoteCount { get; set; }
        public bool WasCorrect { get; set; }
        public float LastActionTime { get; set; }
    }
}
