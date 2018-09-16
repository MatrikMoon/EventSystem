using System.Linq;

/*
 * Created by Moon on 9/11/2018
 * A simple class to map "stock" songIds to their corresponding song names
 */

namespace DiscordCommunityShared
{
    public class OstHashToSongName
    {
        private static readonly string[] ostHashes = { "Level1", "Level2", "Level3", "Level4", "Level5", "Level6",
                                "Level7", "Level8", "Level9", "Level10", "Level11"};

        private static readonly string[] ostNames = { "Beat Saber", "Escape", "Lvl Insane", "$100 Bills", "Country Rounds", "Breezer",
                                "Turn Me On", "Balearic Pumping", "Legend", "Commercial Pumping", "Angel Voices"};


        public static string GetOstSongNameFromHash(string levelId)
        {
            levelId = levelId.EndsWith("OneSaber") ? levelId.Substring(0, levelId.IndexOf("OneSaber")) : levelId;
            return ostNames[ostHashes.ToList().IndexOf(levelId)];
        }

        public static bool IsOst(string songId)
        {
            return ostHashes.ToList().Any(x => x == songId || $"{x}OneSaber" == songId);
        }
    }
}
