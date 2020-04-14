using EventPlugin.Misc;
using EventPlugin.Models;
using EventPlugin.UI;
using EventShared;
using IPA;
using UnityEngine.SceneManagement;

namespace EventPlugin
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public string Name => SharedConstructs.Name;
        public string Version => SharedConstructs.Version;

        public static ulong UserId;

        [OnStart]
        public void OnStart()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            EventUI.CreateCommunitiyButton();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.name == "MenuCore")
            {
                Config.LoadConfig();
                Player.GetPlatformUsername((username, id) => UserId = id);
            }
        }
    }
}
