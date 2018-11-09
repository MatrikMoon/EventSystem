/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */ 

namespace DiscordCommunityShared
{
    public static class SharedConstructs
    {
        public static string Name => "DiscordCommunityPlugin";
        public static string Version => "0.1.2";
        public static int VersionCode => 012;
        public static string Changelog =
            "0.0.1: First Commit. Base Discord/Plugin/Server functional, some commands. Just added \"mode\' differentiation between songs.\n" +
            "0.0.2: Added Oculus support. Added Leaderboard views.\n" +
            "0.0.3: Flower Dance temp patch\n" +
            "0.0.4: Fixed leaderboard \"royally screwed\" bug\n" +
            "0.0.5: Sooper Secret Settings\n" +
            "0.0.6: First open (forced) release to the Discord server\n" +
            "0.0.7: Added Mirror mode\n" +
            "0.0.8: Added stricter registration policy\n" +
            "0.0.9: Added scrollable leaderboards\n" +
            "0.1.0: Bugfixes with switchable leaderboard view\n" +
            "0.1.1: Colored leaderboards and projected token view!\n" +
            "0.1.2: Token/Rank system implemented! Mirror/Static lights moved to settings";

        public enum Rank
        {
            None = -1, //Not to be stored. Only use as "does not exist"
            White = 0,
            Bronze = 1,
            Silver = 2,
            Gold = 3,
            Blue = 4,
            Master = 5,
            All = 6 //Not to be stored. Only use as "no filter necessary"
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
