/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */ 

namespace TeamSaberShared
{
    public static class SharedConstructs
    {
        public static string Name => "TeamSaberPlugin";
        public static string Version => "0.0.6";
        public static int VersionCode => 006;
        public static string Changelog =
            "0.0.1: First attempt at fork from DiscordCommunityPlugin\n" +
            "0.0.2: Sample update\n" +
            "0.0.3: Made Teams dynamically loaded from server\n" +
            "0.0.4: Difficulties are now decided by the server, fixed isolation, unified oculus / steam versions\n" +
            "0.0.5: Various UI improvements: Fixed scroll buttons, made plugin button non-interactible until song load, added difficulty text view\n" +
            "0.0.6: Event 2: HoT, again tried to fix unification\n" +
            "0.0.7: Fixed scroll button overlap! Thank almighty andruzzzhka for this miracle\n";

        public enum Rarity
        {
            None = -1, //Only use as "does not exist"
            C = 0,
            B = 1,
            A = 2,
            S = 3,
            SS = 4,
            SSS = 5,
            All = 6 //Not to be stored. Only use as "no filter necessary"
        }

        /*
        public enum Team
        {
            None = -1, //Only use as "does not exist"
            Team1,
            Team2,
            All //Not to be stored. Only use as "no filter necessary"
        }
        */

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
