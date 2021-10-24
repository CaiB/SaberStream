using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaberStream.Data
{
    [Flags]
    public enum Difficulty
    {
        None = 0,
        Easy = 1,
        Normal = 2,
        Hard = 4,
        Expert = 8,
        ExpertPlus = 16,
        All = 31
    }

    public static class DifficultyUtil
    {
        /// <summary>Gets the 1 or 2-character shorthand for the given difficulty.</summary>
        /// <param name="diff">THe difficulty to shorten</param>
        /// <returns>A 1- or 2-character string for the difficulty, or null if the difficulty is not recognized or <see cref="Difficulty.None"/></returns>
        public static string? GetShortName(Difficulty diff)
        {
            if (diff.HasFlag(Difficulty.Easy)) { return "E"; }
            if (diff.HasFlag(Difficulty.Normal)) { return "N"; }
            if (diff.HasFlag(Difficulty.Hard)) { return "H"; }
            if (diff.HasFlag(Difficulty.Expert)) { return "X"; }
            if (diff.HasFlag(Difficulty.ExpertPlus)) { return "X+"; }
            return null;
        }

        /// <summary>Converts a difficulty name into a <see cref="Difficulty"/></summary>
        /// <param name="diff">The difficulty name to parse</param>
        /// <returns>The difficulty in the input, or <see cref="Difficulty.None"/> if not recognized</returns>
        public static Difficulty Parse(string? diff)
        {
            if (diff == null) { return Difficulty.None; }
            if (diff == "Easy") { return Difficulty.Easy; }
            if (diff == "Normal") { return Difficulty.Normal; }
            if (diff == "Hard") { return Difficulty.Hard; }
            if (diff == "Expert") { return Difficulty.Expert; }
            if (diff == "ExpertPlus" || diff == "Expert+") { return Difficulty.ExpertPlus; }
            return Difficulty.None;
        }
    }
}
