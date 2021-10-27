using System;

namespace SaberStream.Data
{
    public enum HistoryType
    {
        /// <summary>
        /// One or more blocks was hit correctly.
        /// NoteCount will store the relevant combo.
        /// EndTime will have the time of the last hit in the series.
        /// </summary>
        Hit,

        /// <summary>
        /// One or more mistakes was made. This could be a miss, incorrect cut, hitting a bomb, or entering a wall.
        /// NoteCount will store the number of consecutive mistakes.
        /// EndTime will have the time of the last mistake in the series.
        /// </summary>
        Mistake,

        /// <summary>
        /// The player failed the level.
        /// NoteCount is meaningless.
        /// EndTime will store the time when the player re-joined the level, or the time of failure if they have not rejoined.
        /// </summary>
        LevelFail,

        /// <summary>
        /// The player started playing the level (at the beginning, or partway through after a fail/exit).
        /// NoteCount is meaningless.
        /// EndTime will store the time when the player joined the level.
        /// NewDifficulty and NewGamemode will be populated.
        /// </summary>
        Join
    }

    public class HistoryEntry
    {
        /// <summary>How many notes are involved, if this is a hit or miss streak</summary>
        public int NoteCount { get; set; }

        /// <summary>When the streak ended or the player rejoined after a fail</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>What this entry represents</summary>
        public HistoryType Type { get; set; }

        /// <summary>Contains the difficulty the player is playing at starting now.</summary>
        /// <remarks>Only set when <see cref="Type"/> is <see cref="HistoryType.Join"/>.</remarks>
        public Difficulty NewDifficulty { get; set; } = Difficulty.None;

        // public Gamemode NewGamemode { get; set; } = Gamemode.None;
    }
}
