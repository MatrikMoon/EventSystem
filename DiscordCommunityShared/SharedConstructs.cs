/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */ 

namespace DiscordCommunityShared
{
    public static class SharedConstructs
    {
        public static string Name => "DiscordCommunityPlugin";
        public static string Version => "0.0.1";
        public static int VersionCode => 001;

        public enum Rank
        {
            White = 0,
            Bronze = 1,
            Silver = 2,
            Gold = 3,
            Blue = 4,
            Purple = 5
        }

        public enum LevelDifficulty
        {
            Easy = 0,
            Normal = 1,
            Hard = 2,
            Expert = 3,
            ExpertPlus = 4
        }

        public enum GameplayMode
        {
            SoloStandard = 0,
            SoloOneSaber = 1,
            SoloNoArrows = 2,
            PartyStandard = 3
        }
    }
}
