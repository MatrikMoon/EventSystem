using DiscordCommunityShared.SimpleJSON;
using System;
using System.IO;

namespace DiscordCommunityServer
{
    class Config
    {
        public static string BotToken { get; set; }
        public static string BetaBotToken { get; set; }

        private static string ConfigLocation = $"{Environment.CurrentDirectory}/Config.txt";

        public static void LoadConfig()
        {
            if (File.Exists(ConfigLocation))
            {
                JSONNode node = JSON.Parse(File.ReadAllText(ConfigLocation));
                BotToken = node["BotToken"].Value;
                BetaBotToken = node["BetaBotToken"].Value;
            }
        }

        public static void SaveConfig()
        {
            JSONNode node = new JSONObject();
            node["BotToken"] = BotToken;
            node["BetaBotToken"] = BetaBotToken;
            File.WriteAllText(ConfigLocation, node.ToString());
        }
    }
}
