using DiscordCommunityPlugin.UI;
using DiscordCommunityPlugin.UI.FlowCoordinators;
using DiscordCommunityPlugin.UI.ViewControllers;
using Oculus.Platform;
using Oculus.Platform.Models;
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

        //Called on Menu scene load
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

                Users.GetLoggedInUser().OnComplete((Message<User> msg) =>
                {
                    Plugin.PlayerId = msg.Data.ID;
                });

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
                    if (!ReflectionUtil.GetLoadedAssemblies().Contains("SongLoaderPlugin"))
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

        IEnumerator ReturnToCommunityUI(string selectedLevelId = null)
        {
            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<VRUIScreenSystem>().Any(); });
            VRUIScreenSystem screenSystem = Resources.FindObjectsOfTypeAll<VRUIScreenSystem>().First();

            yield return new WaitWhile(delegate () { return screenSystem.mainScreen == null; });
            yield return new WaitWhile(delegate () { return screenSystem.mainScreen.rootViewController == null; });

            try
            {

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