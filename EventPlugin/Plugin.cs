using EventPlugin.Misc;
using EventPlugin.Models;
using EventPlugin.UI;
using EventShared;
using IPA;
using UnityEngine.SceneManagement;

namespace EventPlugin
{
    public class Plugin : IBeatSaberPlugin
    {
        public string Name => SharedConstructs.Name;
        public string Version => SharedConstructs.Version;

        public static ulong UserId;

        public void OnApplicationStart()
        {
            EventUI.CreateCommunitiyButton();
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

        private static int framesUntilPrint = 900;
        private static int currentFrameCount = 0;
        private static float totalTime = 0;

        public void OnUpdate()
        {
            /*currentFrameCount++;
            totalTime += Time.deltaTime;
            if (currentFrameCount >= framesUntilPrint)
            {
                EventShared.Logger.Debug($"TOOK {totalTime} TO RUN {framesUntilPrint} FRAMES; FPS: {framesUntilPrint / totalTime}");
                currentFrameCount = 0;
                totalTime = 0;
            }*/
        }

        private static int framesUntilPrintF = 900;
        private static int currentFrameCountF = 0;
        private static float totalTimeF = 0;

        public void OnFixedUpdate()
        {
            /*currentFrameCountF++;
            totalTimeF += Time.deltaTime;
            if (currentFrameCountF >= framesUntilPrintF)
            {
                EventShared.Logger.Debug($"TOOK {totalTimeF} TO RUN {framesUntilPrintF} FRAMES; (FIXED) FPS: {framesUntilPrintF / totalTimeF}");
                currentFrameCountF = 0;
                totalTimeF = 0;
            }*/
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.name == "MenuCore")
            {
                Config.LoadConfig();
                Player.GetPlatformUsername((username, id) => UserId = id);
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
