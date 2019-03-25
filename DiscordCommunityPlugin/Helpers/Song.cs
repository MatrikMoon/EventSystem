using static TeamSaberShared.SharedConstructs;

/**
 * Created by Moon on 3/8/2019
 * Tiny helper class to hold together levels and desired options
 */

namespace TeamSaberPlugin.Helpers
{
    class Song
    {
        public string SongId { get; set; }
        public string SongName { get; set; }
        public LevelDifficulty Difficulty { get; set; }
        public IDifficultyBeatmap Beatmap { get; set; }
        public GameOptions GameOptions { get; set; }
        public PlayerOptions PlayerOptions { get; set; }

        //TEMPORARY - TeamSaber
        public float Speed { get; set; }

        //Necessary overrides for being used as a key in a Dictionary
        public static bool operator ==(Song a, Song b)
        {
            if (b == null) return false;
            return a.GetHashCode() == b.GetHashCode();
        }

        public static bool operator !=(Song a, Song b)
        {
            if (b == null) return false;
            return a.GetHashCode() != b.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is Song)) return false;
            return GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + SongId.GetHashCode();
            hash = (hash * 7) + Difficulty.GetHashCode();
            return hash;
        }
        //End necessary overrides
    }
}
