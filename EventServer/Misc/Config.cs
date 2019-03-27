using TeamSaberShared.SimpleJSON;
using System;
using System.IO;
using static TeamSaberShared.SharedConstructs;

namespace EventServer
{
    class Config
    {
        public static string BotToken { get; set; }
        public static string BetaBotToken { get; set; }
        public static ServerFeatures ServerFlags { get; set; }

        private static string ConfigLocation = $"{Environment.CurrentDirectory}/Config.txt";

        public static void LoadConfig()
        {
            if (File.Exists(ConfigLocation))
            {
                JSONNode node = JSON.Parse(File.ReadAllText(ConfigLocation));
                BotToken = node["BotToken"].Value;
                BetaBotToken = node["BetaBotToken"].Value;
                ServerFlags = (ServerFeatures)Convert.ToInt32(node["ServerFlags"].Value);
            }
            else
            {
                BotToken = "[ReleaseToken]";
                BetaBotToken = "[BetaToken]";
                ServerFlags = 0;
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            JSONNode node = new JSONObject();
            node["BotToken"] = BotToken;
            node["BetaBotToken"] = BetaBotToken;
            node["ServerFlags"] = (int)ServerFlags;
            File.WriteAllText(ConfigLocation, node.ToString());
        }
    }
}
