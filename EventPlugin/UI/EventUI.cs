using BeatSaberMarkupLanguage.MenuButtons;
using EventPlugin.Misc;
using EventPlugin.Models;
using EventPlugin.UI.FlowCoordinators;
using SongCore;
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

namespace EventPlugin.UI
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class EventUI : MonoBehaviour
    {
        public static EventUI instance;

        public MainModFlowCoordinator _mainModFlowCoordinator; //TODO: Temporarily public, for nofail toggle
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
            new GameObject("EventPlugin").AddComponent<EventUI>();
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

                Player.GetPlatformUsername((username, id) => Plugin.UserId = id);

                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                Loader.SongsLoadedEvent += SongsLoaded;
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

        private void SongsLoaded(Loader _, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            if (_communityButton != null) _communityButton.Interactable = true;
        }

        private void CreateCommunitiyButton()
        {
            _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            if (_mainModFlowCoordinator == null)
            {
                _mainModFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<MainModFlowCoordinator>(_mainFlowCoordinator.gameObject);
                _mainModFlowCoordinator.mainFlowCoordinator = _mainFlowCoordinator;
            }

            try
            {
                if (ReflectionUtil.ListLoadedAssemblies().Any(x => x.GetName().Name == "SongCore"))
                {
#if TEAMSABER
                    var buttonName = "Team Saber";
                    var hint = "Compete with your team in the competition!";
#elif DISCORDCOMMUNITY
                    var buttonName = "Discord Community";
                    var hint = "Compete with your team in the competition!";
#elif ASIAVR
                    var buttonName = "AsiaVR Tournament";
                    var hint = "Compete in the Asia Server Event!";
#else
                    var buttonName = "EVENT BETA";
                    var hint = "STILL A BETA";
#endif
                    //_communityButton = MenuButtonUI.AddButton(buttonName, hint, () => _mainModFlowCoordinator.PresentMainModUI());
                    _communityButton = new MenuButton(buttonName, hint, () => _mainModFlowCoordinator.PresentMainModUI());
                    _communityButton.Interactable = Loader.AreSongsLoaded;
                    MenuButtons.instance.RegisterButton(_communityButton);
                }
                else Logger.Error("MISSING SONGCORE PLUGIN");
            }
            catch (Exception e)
            {
                Logger.Error("Error: " + e.Message);
                Logger.Error(e.StackTrace);
            }
        }
    }
}
