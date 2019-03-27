using TeamSaberShared.SimpleJSON;
using System;
using System.IO;
using System.Reflection;
using TeamSaberShared;

namespace TeamSaberPlugin.Misc
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

        private static float _speed;
        public static float Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
                SaveConfig();
            }
        }

        private static string ConfigLocation = $"{Environment.CurrentDirectory}/UserData/TeamSaber.txt";

        public static void LoadConfig()
        {
            if (File.Exists(ConfigLocation))
            {
                JSONNode node = JSON.Parse(File.ReadAllText(ConfigLocation));
                SooperSecretSetting = Convert.ToBoolean(node["SooperSecretSetting"].Value);
                MirrorMode = Convert.ToBoolean(node["Mirror"].Value);
                StaticLights = Convert.ToBoolean(node["StaticLights"].Value);
                Speed = float.Parse(node["Speed"].Value);
            }
            else
            {
                //TODO: Do we even need these?
                SooperSecretSetting = false;
                MirrorMode = false;
                StaticLights = false;
                Speed = 1f;
            }
        }

        public static void SaveConfig()
        {
            JSONNode node = new JSONObject();
            node["SooperSecretSetting"] = SooperSecretSetting;
            node["Mirror"] = MirrorMode;
            node["StaticLights"] = StaticLights;
            node["Speed"] = Speed;
            File.WriteAllText(ConfigLocation, node.ToString());
        }
    }
}
