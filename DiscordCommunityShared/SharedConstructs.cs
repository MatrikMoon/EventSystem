/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */ 

namespace TeamSaberShared
{
    public static class SharedConstructs
    {
        public static string Name => "TeamSaberPlugin";
        public static string Version => "0.0.2";
        public static int VersionCode => 002;
        public static string Changelog =
            "0.0.1: First attempt at fork from DiscordCommunityPlugin\n" +
            "0.0.2: Sample update\n";

        public enum Rarity
        {
            None = -1, //Only use as "does not exist"
            Uncommon = 0,
            Rare = 1,
            Epic = 2,
            Legendary = 3,
            Mythic = 4,
            Captain = 5,
            All = 6 //Not to be stored. Only use as "no filter necessary"
        }

        public enum Team
        {
            None = -1, //Only use as "does not exist"
            Team1,
            Team2,
            All //Not to be stored. Only use as "no filter necessary"
        }

        public enum LevelDifficulty
        {
            Easy = 0,
            Normal = 1,
            Hard = 2,
            Expert = 3,
            ExpertPlus = 4
        }
    }
}
