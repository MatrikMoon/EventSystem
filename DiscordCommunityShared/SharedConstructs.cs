/*
 * Created by Moon on 9/15/2018
 * Holds simple static variables and constructs needed by both sides of the plugin
 */ 

namespace DiscordCommunityShared
{
    public static class SharedConstructs
    {
        public static string Name => "ChristmasVotingPlugin";
        public static string Version => "0.0.1";
        public static int VersionCode => 001;

        public enum Category
        {
            None = -1, //Not to be stored. Only use as "does not exist"
            Map = 0,
            Saber = 1,
            Avatar = 2,
            Platform = 3
        }
    }
}
