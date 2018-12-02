using CustomUI.BeatSaber;
using DiscordCommunityPlugin.UI;
using DiscordCommunityShared;
using IllusionPlugin;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = DiscordCommunityShared.Logger;

namespace DiscordCommunityPlugin
{
    public class Plugin : IPlugin
    {
        public string Name => SharedConstructs.Name;
        public string Version => SharedConstructs.Version;
        public static ulong PlayerId;

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            //SharedCoroutineStarter.instance.StartCoroutine(WaitForBuildMode());
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            if (scene.name == "Menu")
            {
                CommunityUI.OnLoad();
            }
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        /* Trigger press detection code, for debugging UI element positioning
        private static bool rightDown = false;
        private static Action triggerPressed;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton15) && !rightDown)
            {
                rightDown = true;
                triggerPressed?.Invoke();
            }
            if (Input.GetKeyUp(KeyCode.JoystickButton15) && rightDown) rightDown = false;
        }
        */
    }
}
