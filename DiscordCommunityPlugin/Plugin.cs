﻿using DiscordCommunityShared;
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

        /*
        private IEnumerator WaitForLevelSelection()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().Any());
            Logger.Error("FLOWCOORDINATOR CREATED, ADDING/REMOVING CALLBACKS");
            StandardLevelSelectionFlowCoordinator slsfc = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
            StandardLevelListViewController slsvc = slsfc.GetField<StandardLevelListViewController>("_levelListViewController");
            StandardLevelDifficultyViewController sldvc = slsfc.GetField<StandardLevelDifficultyViewController>("_levelDifficultyViewController");
            slsvc.didSelectLevelEvent -= slsfc.HandleLevelListViewControllerDidSelectLevel;
            sldvc.didSelectDifficultyEvent -= slsfc.HandleDifficultyViewControllerDidSelectDifficulty;
            slsvc.didSelectLevelEvent += HandleLevelListViewControllerDidSelectLevel;
            sldvc.didSelectDifficultyEvent += HandleDifficultyViewControllerDidSelectDifficulty;
        }

        public void HandleLevelListViewControllerDidSelectLevel(StandardLevelListViewController viewController, IStandardLevel level)
        {
            Logger.Warning("LEVEL SELECTED");
        }

        public void HandleDifficultyViewControllerDidSelectDifficulty(StandardLevelDifficultyViewController viewController, IStandardLevelDifficultyBeatmap difficultyLevel)
        {
            Logger.Warning("DIFFICULTY SELECTED");
        }

        private IEnumerator WaitForBuildMode()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<GameBuildMode>().Any());
            Logger.Error("SETTING BUILD TO DEMO");
            GameBuildMode buildMode = Resources.FindObjectsOfTypeAll<GameBuildMode>().First();
            buildMode.ForceSetMode(GameBuildMode.Mode.Demo);
        }
        */

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            if (scene.name == "Menu")
            {
                BaseUI.OnLoad();
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
    }
}
