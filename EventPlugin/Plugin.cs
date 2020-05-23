using EventPlugin.Misc;
using EventPlugin.Models;
using EventPlugin.UI;
using EventShared;
using IPA;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EventPlugin
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public string Name => SharedConstructs.Name;
        public string Version => SharedConstructs.Version;

        public static ulong UserId;

        private static UnityMainThreadDispatcher _threadDispatcher;

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
                _threadDispatcher = _threadDispatcher ?? new GameObject("Thread Dispatcher").AddComponent<UnityMainThreadDispatcher>();

                Config.LoadConfig();
                Player.GetPlatformUsername((username, id) => UserId = id);
            }
        }
    }
}
