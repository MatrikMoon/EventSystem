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
        public static readonly string[] ostHashes = { "BeatSaber", "Escape", "LvlInsane", "100Bills", "CountryRounds", "Breezer",
                                "TurnMeOn", "BalearicPumping", "Legend", "CommercialPumping", "AngelVoices", "OneHope"};

        public static readonly string[] ostNames = { "Beat Saber", "Escape", "Lvl Insane", "$100 Bills", "Country Rounds", "Breezer",
                                "Turn Me On", "Balearic Pumping", "Legend", "Commercial Pumping", "Angel Voices", "One Hope"};

        //C# doesn't seem to want me to use an array of a non-primitive here.
        private static readonly int[] mainDifficulties = { (int)LevelDifficulty.Easy, (int)LevelDifficulty.Normal, (int)LevelDifficulty.Hard, (int)LevelDifficulty.Expert, (int)LevelDifficulty.ExpertPlus };
        private static readonly int[] angelDifficulties = { (int)LevelDifficulty.Hard, (int)LevelDifficulty.Expert };
        private static readonly int[] oneSaberDifficulties = { (int)LevelDifficulty.Expert };
        private static readonly int[] noArrowsDifficulties = { (int)LevelDifficulty.Expert };

        public static string GetOstSongNameFromLevelId(string levelId)
        {
            levelId = levelId.EndsWith("OneSaber") ? levelId.Substring(0, levelId.IndexOf("OneSaber")) : levelId;
            levelId = levelId.EndsWith("NoArrows") ? levelId.Substring(0, levelId.IndexOf("NoArrows")) : levelId;
            return ostNames[ostHashes.ToList().IndexOf(levelId)];
        }

        public static LevelDifficulty[] GetDifficultiesFromLevelId(string levelId)
        {
            if (IsOst(levelId))
            {
                if (levelId.Contains("OneSaber")) return oneSaberDifficulties.Select(x => (LevelDifficulty)x).ToArray();
                else if (levelId.Contains("NoArrows")) return noArrowsDifficulties.Select(x => (LevelDifficulty)x).ToArray();
                else if (levelId != "AngelVoices") return mainDifficulties.Select(x => (LevelDifficulty)x).ToArray();
                else return angelDifficulties.Select(x => (LevelDifficulty)x).ToArray();
            }
            return null;
        }

        public static bool IsOst(string songId)
        {
            return ostHashes.ToList().Any(x => x == songId || $"{x}OneSaber" == songId || $"{x}NoArrows" == songId);
        }
    }
}
