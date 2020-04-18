using System.Reflection;
using static EventShared.SharedConstructs;

/**
 * Created by Moon on 3/8/2019
 * Tiny helper class to hold together levels and desired options
 */

namespace EventPlugin.Models
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Song
    {
        public string Hash { get; set; }
        public string SongName { get; set; }
        public LevelDifficulty Difficulty { get; set; }
        public string Characteristic { get; set; }

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public IPreviewBeatmapLevel PreviewBeatmap { get; set; }

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public IDifficultyBeatmap Beatmap { get; set; }

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public GameOptions GameOptions { get; set; }

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public PlayerOptions PlayerOptions { get; set; }

        //Necessary overrides for being used as a key in a Dictionary
        public static bool operator ==(Song a, Song b)
        {
            if (ReferenceEquals(b, null)) return false;
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
            hash = (hash * 7) + Hash.GetHashCode();
            hash = (hash * 7) + Difficulty.GetHashCode();
            return hash;
        }
        //End necessary overrides
    }
}
