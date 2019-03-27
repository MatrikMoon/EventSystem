﻿using System.Collections.Generic;
using System.Linq;
using static TeamSaberShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * A simple class to map "stock" songIds to their corresponding song names
 * TODO: Properly handle different map types like "OneSaber" and maps without all difficulties
 */

namespace TeamSaberShared
{
    public class OstHelper
    {
        static OstHelper()
        {
            packs = new Pack[] {
                new Pack
                {
                    PackID = "OstVol1",
                    PackName = "Original Soundtrack Vol. 1",
                    SongDictionary = new Dictionary<string, string>
                    {
                        { "BeatSaber", "Beat Saber" },
                        { "Escape", "Escape" },
                        { "LvlInsane", "Lvl Insane" },
                        { "100Bills", "$100 Bills" },
                        { "CountryRounds", "Country Rounds" },
                        { "Breezer", "Breezer" },
                        { "TurnMeOn", "Turn Me On" },
                        { "BalearicPumping", "Balaeric Pumping" },
                        { "Legend", "Legend" },
                        { "CommercialPumping", "Commercial Pumping" }
                    }
                },
                new Pack
                {
                    PackID = "OstVol2",
                    PackName = "Original Soundtrack Vol. 2",
                    SongDictionary = new Dictionary<string, string>
                    {
                        { "BeThereForYou", "Be There For You" },
                        { "Elixia", "Elixia" },
                        { "INeedYou", "I Need You" },
                        { "RumNBass", "Rum n' Bass" },
                        { "UnlimitedPower", "Unlimited Power" }
                    }
                },
                new Pack
                {
                    PackID = "Extras",
                    PackName = "Extras",
                    SongDictionary = new Dictionary<string, string>
                    {
                        { "AngelVoices", "Angel Voices" },
                        { "OneHope", "One Hope" },
                        { "PopStars", "POP/STARS - K/DA" }
                    }
                }
            };

            foreach (Pack pack in packs)
            {
                allLevels = allLevels.Concat(pack.SongDictionary).ToDictionary(s => s.Key, s => s.Value);
            }
        }

        public class Pack
        {
            public string PackID { get; set; }
            public string PackName { get; set; }
            public Dictionary<string, string> SongDictionary { get; set; }
        }

        public static readonly Pack[] packs;
        public static readonly Dictionary<string, string> allLevels = new Dictionary<string, string>();

        //C# doesn't seem to want me to use an array of a non-primitive here.
        private static readonly int[] mainDifficulties = { (int)LevelDifficulty.Easy, (int)LevelDifficulty.Normal, (int)LevelDifficulty.Hard, (int)LevelDifficulty.Expert, (int)LevelDifficulty.ExpertPlus };
        private static readonly int[] angelDifficulties = { (int)LevelDifficulty.Hard, (int)LevelDifficulty.Expert, (int)LevelDifficulty.ExpertPlus };
        private static readonly int[] oneSaberDifficulties = { (int)LevelDifficulty.Expert };
        private static readonly int[] noArrowsDifficulties = { (int)LevelDifficulty.Expert };

        public static string GetOstSongNameFromLevelId(string levelId)
        {
            levelId = levelId.EndsWith("OneSaber") ? levelId.Substring(0, levelId.IndexOf("OneSaber")) : levelId;
            levelId = levelId.EndsWith("NoArrows") ? levelId.Substring(0, levelId.IndexOf("NoArrows")) : levelId;
            return allLevels[levelId];
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

        public static bool IsOst(string levelId)
        {
            levelId = levelId.EndsWith("OneSaber") ? levelId.Substring(0, levelId.IndexOf("OneSaber")) : levelId;
            levelId = levelId.EndsWith("NoArrows") ? levelId.Substring(0, levelId.IndexOf("NoArrows")) : levelId;
            return packs.Any(x => x.SongDictionary.ContainsKey(levelId));
        }
    }
}