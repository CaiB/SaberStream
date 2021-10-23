using System;

namespace SaberStream.Data
{
    public record DifficultyInfo
    {
        public Difficulty Difficulty { get; init; }

        public int NoteCount { get; init; }
        public int BombCount { get; init; }
        public int WallCount { get; init; }

        public DifficultyInfo(Difficulty diff) { this.Difficulty = diff; }
    }
}
