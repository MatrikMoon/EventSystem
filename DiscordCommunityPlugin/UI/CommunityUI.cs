using DiscordCommunityPlugin.Misc;
using DiscordCommunityPlugin.UI;
using DiscordCommunityPlugin.UI.FlowCoordinators;
using DiscordCommunityPlugin.UI.ViewControllers;
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

namespace DiscordCommunityPlugin
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CommunityUI : MonoBehaviour
    {
        public static CommunityUI instance;
        public string communitySongPlayed;

        private MainModFlowCoordinator _mainFlowCooridnator;
        private RectTransform _mainMenuRectTransform;
        private MainMenuViewController _mainMenuViewController;
        private ModalViewController _requiredModsModal;
        private Button _communityButton;

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
                Config.LoadConfig();

                Plugin.PlayerId = Steamworks.SteamUser.GetSteamID().m_SteamID;

                SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
                SongLoader.SongsLoadedEvent += SongsLoaded;
                CreateCommunitiyButton();
            }
        }

        private void SceneManager_activeSceneChanged(Scene prev, Scene next)
        {
            if (next.name == "Menu")
            {
                if (communitySongPlayed != null)
                {
                    StartCoroutine(ReturnToCommunityUI(communitySongPlayed));
                    communitySongPlayed = null;
                }
                else CreateCommunitiyButton();
            }
        }

        private void SongsLoaded(SongLoader sender, List<SongLoaderPlugin.OverrideClasses.CustomLevel> loadedSongs)
        {
            if (_communityButton != null) _communityButton.interactable = true;
        }

        private void CreateCommunitiyButton()
        {
            CreateSettingsMenu();

            //Multiplayer score hook
            if (Config.SooperSecretSetting)
            {
                StartCoroutine(Hooks.WaitForMultiplayerLevelComplete());
            }

            _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
            _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;
            if (_mainFlowCooridnator == null)
            {
                _mainFlowCooridnator = new GameObject("MainModFlow").AddComponent<MainModFlowCoordinator>();
                _mainFlowCooridnator.mmvc = _mainMenuViewController;
            }

            _communityButton = BaseUI.CreateUIButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                (_communityButton.transform as RectTransform).anchoredPosition = new Vector2(61f, 5f);
                (_communityButton.transform as RectTransform).sizeDelta = new Vector2(38f, 10f);
                _communityButton.interactable = SongLoader.AreSongsLoaded;

                BaseUI.SetButtonText(_communityButton, "DiscordCommunity");

                _communityButton.onClick.AddListener(() => {
                    //If the user doesn't have the songloader plugin installed, we definitely can't continue
                    if (!ReflectionUtil.ListLoadedAssemblies().Any(x => x.GetName().Name == "SongLoaderPlugin"))
                    {
                        _requiredModsModal = BaseUI.CreateViewController<ModalViewController>();
                        _requiredModsModal.Message = "You do not have the following required mods installed:\n" +
                        "SongLoaderPlugin\n\n" +
                        "DiscordCommunityPlugin will not function.";
                        _requiredModsModal.Type = ModalViewController.ModalType.Ok;
                        _mainMenuViewController.PresentModalViewController(_requiredModsModal, null, _mainMenuViewController.isRebuildingHierarchy);
                    }
                    else
                    {
                        _mainFlowCooridnator.PresentMainModUI();
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error("Error: " + e.Message);
                Logger.Error(e.StackTrace);
            }
        }

        private void CreateSettingsMenu()
        {
            var subMenu = SettingsUI.CreateSubMenu("Community Plugin");
            var sooperSecretSetting = subMenu.AddBool("Sooper Secret Setting");
            sooperSecretSetting.GetValue += delegate { return Config.SooperSecretSetting; };
            sooperSecretSetting.SetValue += delegate (bool value) { Config.SooperSecretSetting = value; };

            var ctOption = GameOptionsUI.CreateToggleOption("ChromaToggle");
            ctOption.OnToggle += (e) => Logger.Info($"ONTOGGLE: {e}");
            GameOptionsUI.Build();
        }

        //Returns to a view when the scene loads, courtesy of andruzzzhka's BeatSaberMultiplayer
        IEnumerator ReturnToCommunityUI(string selectedLevelId = null)
        {
            //Wait for screen system to load completely
            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<VRUIScreenSystem>().Any(); });
            VRUIScreenSystem screenSystem = Resources.FindObjectsOfTypeAll<VRUIScreenSystem>().First();

            yield return new WaitWhile(delegate () { return screenSystem.mainScreen == null; });
            yield return new WaitWhile(delegate () { return screenSystem.mainScreen.rootViewController == null; });

            try
            {
                //What the hell andruzzzhka
                //I'm pretty sure this dismisses all viewcontrollers from the bottom
                //of the tree up to root, not including root. Not sure what that means though.
                VRUIViewController root = screenSystem.mainScreen.rootViewController;

                List<VRUIViewController> children = new List<VRUIViewController>();

                children.Add(root);

                while (children.Last().childViewController != null)
                {
                    children.Add(children.Last().childViewController);
                }

                children.Reverse();
                children.Remove(root);
                children.ForEach(x => {
                    Logger.Info($"Dismissing {x.name}...");
                    x.DismissModalViewController(null, true);
                });

                //Re-add the button and move to the song list
                CreateCommunitiyButton();
                _mainFlowCooridnator.mmvc = _mainMenuViewController;
                _mainFlowCooridnator.PresentMainModUI(true, selectedLevelId);
            }
            catch (Exception e)
            {
                Logger.Error($"MENU EXCEPTION: {e}");
            }
        }
    }
}