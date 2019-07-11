using EventShared;
using UnityEngine.SceneManagement;
using EventPlugin.UI;
using IPA;

namespace EventPlugin
{
    public class Plugin : IBeatSaberPlugin
    {
        public string Name => SharedConstructs.Name;
        public string Version => SharedConstructs.Version;

        public static ulong UserId;

        public void OnApplicationStart()
        {
        }


        public void OnApplicationQuit()
        {
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

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.name == "MenuCore")
            {
                EventUI.OnLoad();
            }
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
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
