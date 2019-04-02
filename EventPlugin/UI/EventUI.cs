using CustomUI.MenuButton;
using CustomUI.Settings;
using EventPlugin.Helpers;
using EventPlugin.Misc;
using EventPlugin.UI.FlowCoordinators;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = EventShared.Logger;

/**
 * Created by Moon on 8/23/2018
 * Serves as the main UI class for the Plugin
 * Heavily influenced by BeatSaverDownloader
 * (https://github.com/andruzzzhka/BeatSaverDownloader/)
 */

namespace EventPlugin
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class EventUI : MonoBehaviour
    {
        public static EventUI instance;
        public string songPlayed; //TODO: Obselete? It's no longer used because ReturnToUI is gone

        private MainModFlowCoordinator _mainModFlowCoordinator;
        private RectTransform _mainMenuRectTransform;
        private MainFlowCoordinator _mainFlowCoordinator;
        private MainMenuViewController _mainMenuViewController;
        private MenuButton _communityButton;

        //Called on Menu scene load (only once in lifetime)
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public static void OnLoad()
        {
            if (instance != null)
            {
                return;
            }
            new GameObject("TeamSaber Plugin").AddComponent<EventUI>();
        }

        //Called on object creation (only once in lifetime)
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public void Awake()
        {
            if (instance != this)
            {
                instance = this;
                DontDestroyOnLoad(this);
                Config.LoadConfig();

                Player.UpdateUserId();

                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                SongLoader.SongsLoadedEvent += SongsLoaded;
                CreateCommunitiyButton(); //sceneLoaded won't be called the first time
            }
        }

        private void SceneManager_sceneLoaded(Scene next, LoadSceneMode mode)
        {
            if (next.name == "MenuCore")
            {
                StartCoroutine(SetupUI());
            }
        }

        //Waits for menu scenes to be loaded then creates UI elements
        //Courtesy of BeatSaverDownloader
        private IEnumerator SetupUI()
        {
            List<Scene> menuScenes = new List<Scene>() { SceneManager.GetSceneByName("MenuCore"), SceneManager.GetSceneByName("MenuViewControllers"), SceneManager.GetSceneByName("MainMenu") };
            yield return new WaitUntil(() => { return menuScenes.All(x => x.isLoaded); });

            CreateCommunitiyButton();
        }

        private void SongsLoaded(SongLoader sender, List<SongLoaderPlugin.OverrideClasses.CustomLevel> loadedSongs)
        {
            if (_communityButton != null) _communityButton.interactable = true;
        }

        private void CreateCommunitiyButton()
        {
            CreateSettingsMenu();

            _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
            _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;
            if (_mainModFlowCoordinator == null)
            {
                _mainModFlowCoordinator = _mainFlowCoordinator.gameObject.AddComponent<MainModFlowCoordinator>();
                _mainModFlowCoordinator.mfc = _mainFlowCoordinator;
                _mainModFlowCoordinator.mmvc = _mainMenuViewController;
            }

            try
            {
                if (ReflectionUtil.ListLoadedAssemblies().Any(x => x.GetName().Name == "SongLoaderPlugin"))
                {
                    _communityButton = MenuButtonUI.AddButton("Team Saber", "Compete with your team in the competition!", () => _mainModFlowCoordinator.PresentMainModUI());
                    _communityButton.interactable = SongLoader.AreSongsLoaded;
                }
                else Logger.Error("MISSING SONG LOADER PLUGIN");
            }
            catch (Exception e)
            {
                Logger.Error("Error: " + e.Message);
                Logger.Error(e.StackTrace);
            }
        }

        private void CreateSettingsMenu()
        {

        }
    }
}
