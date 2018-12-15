using CustomUI.BeatSaber;
using CustomUI.MenuButton;
using CustomUI.Settings;
using ChristmasVotePlugin.Misc;
using ChristmasVotePlugin.UI;
using ChristmasVotePlugin.UI.FlowCoordinators;
using ChristmasVotePlugin.UI.ViewControllers;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRUI;
using Logger = DiscordCommunityShared.Logger;

/**
 * Created by Moon on 8/23/2018
 * Serves as the main UI class for the Plugin
 * Heavily influenced by BeatSaverDownloader
 * (https://github.com/andruzzzhka/BeatSaverDownloader/)
 */

namespace ChristmasVotePlugin
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CommunityUI : MonoBehaviour
    {
        public static CommunityUI instance;

        private MainModFlowCoordinator _mainModFlowCoordinator;
        private RectTransform _mainMenuRectTransform;
        private MainFlowCoordinator _mainFlowCoordinator;
        private MainMenuViewController _mainMenuViewController;
        private Button _communityButton; //TODO: Find a way to grab the button instance so we can disable it

        //Called on Menu scene load (only once in lifetime)
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public static void OnLoad()
        {
            if (instance != null)
            {
                return;
            }
            new GameObject("Discord Community Plugin").AddComponent<CommunityUI>();
        }

        //Called on object creation (only once in lifetime)
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public void Awake()
        {
            if (instance != this)
            {
                instance = this;
                DontDestroyOnLoad(this);

                Plugin.PlayerId = Steamworks.SteamUser.GetSteamID().m_SteamID;

                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                SongLoader.SongsLoadedEvent += SongsLoaded;
                CreateCommunitiyButton(); //sceneLoaded won't be called the first time
            }
        }

        private void SceneManager_sceneLoaded(Scene next, LoadSceneMode mode)
        {
            if (next.name == "Menu") CreateCommunitiyButton();
        }

        private void SongsLoaded(SongLoader sender, List<SongLoaderPlugin.OverrideClasses.CustomLevel> loadedSongs)
        {
            if (_communityButton != null) _communityButton.interactable = true;
        }

        private void CreateCommunitiyButton()
        {
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
                    MenuButtonUI.AddButton("DiscordCommunity", () => _mainModFlowCoordinator.PresentMainModUI());
                }
                else Logger.Error("MISSING SONG LOADER PLUGIN");
            }
            catch (Exception e)
            {
                Logger.Error("Error: " + e.Message);
                Logger.Error(e.StackTrace);
            }
        }
    }
}