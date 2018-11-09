using DiscordCommunityShared.SimpleJSON;
using System;
using System.IO;
using System.Reflection;

namespace DiscordCommunityPlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Config
    {
        private static bool _sooperSecretSetting;
        public static bool SooperSecretSetting
        {
            get { return _sooperSecretSetting; }
            set
            {
                _sooperSecretSetting = value;
                SaveConfig();
            }
        }

        private static bool _mirrorMode;
        public static bool MirrorMode
        {
            get { return _mirrorMode; }
            set
            {
                _mirrorMode = value;
                SaveConfig();
            }
        }

        private static bool _staticLights;
        public static bool StaticLights
        {
            get { return _staticLights; }
            set
            {
                _staticLights = value;
                SaveConfig();
            }
        }

        private static string ConfigLocation = $"{Environment.CurrentDirectory}/UserData/DiscordCommunityPlugin.txt";

        public static void LoadConfig()
        {
            if (File.Exists(ConfigLocation))
            {
                JSONNode node = JSON.Parse(File.ReadAllText(ConfigLocation));
                SooperSecretSetting = Convert.ToBoolean(node["SooperSecretSetting"].Value);
            }
            else SooperSecretSetting = false;
        }

        public static void SaveConfig()
        {
            JSONNode node = new JSONObject();
            node["SooperSecretSetting"] = SooperSecretSetting;
            node["Mirror"] = SooperSecretSetting;
            node["StaticLights"] = SooperSecretSetting;
            File.WriteAllText(ConfigLocation, node.ToString());
        }
    }
}
