using System.Linq;
using static DiscordCommunityShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * A simple class to map "stock" songIds to their corresponding song names
 */

namespace DiscordCommunityShared
{
    public class OstHelper
    {
        public static readonly string[] ostHashes = { "Level1", "Level2", "Level3", "Level4", "Level5", "Level6",
                                "Level7", "Level8", "Level9", "Level10", "Level11"};

        public static readonly string[] ostNames = { "Beat Saber", "Escape", "Lvl Insane", "$100 Bills", "Country Rounds", "Breezer",
                                "Turn Me On", "Balearic Pumping", "Legend", "Commercial Pumping", "Angel Voices"};

        //C# doesn't seem to want me to use an array of a non-primitive here.
        private static readonly int[] mainDifficulties = { (int)LevelDifficulty.Easy, (int)LevelDifficulty.Normal, (int)LevelDifficulty.Hard, (int)LevelDifficulty.Expert };
        private static readonly int[] angelDifficulties = { (int)LevelDifficulty.Hard, (int)LevelDifficulty.Expert };
        private static readonly int[] oneSaberDifficulties = { (int)LevelDifficulty.Expert };

        public static string GetOstSongNameFromLevelId(string levelId)
        {
            levelId = levelId.EndsWith("OneSaber") ? levelId.Substring(0, levelId.IndexOf("OneSaber")) : levelId;
            return ostNames[ostHashes.ToList().IndexOf(levelId)];
        }

        public static LevelDifficulty[] GetDifficultiesFromLevelId(string levelId)
        {
            if (IsOst(levelId))
            {
                if (levelId.Contains("OneSaber")) return oneSaberDifficulties.Select(x => (LevelDifficulty)x).ToArray();
                else if (levelId != "Level11") return mainDifficulties.Select(x => (LevelDifficulty)x).ToArray();
                else return angelDifficulties.Select(x => (LevelDifficulty)x).ToArray();
            }
            return null;
        }

        public static bool IsOst(string songId)
        {
            return ostHashes.ToList().Any(x => x == songId || $"{x}OneSaber" == songId);
        }
    }
}
