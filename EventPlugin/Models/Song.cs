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

        #region Equality
        public static bool operator ==(Song a, Song b)
        {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return ReferenceEquals(a, null) && ReferenceEquals(b, null);
            return a.GetHashCode() == b.GetHashCode();
        }

        public static bool operator !=(Song a, Song b)
        {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return ReferenceEquals(a, null) ^ ReferenceEquals(b, null);
            return a.GetHashCode() != b.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (!(other is Song)) return false;
            return GetHashCode() == (other as Song).GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Hash.GetHashCode();
            hash = (hash * 7) + Difficulty.GetHashCode();
            return hash;
        }
        #endregion Equality
    }
}
