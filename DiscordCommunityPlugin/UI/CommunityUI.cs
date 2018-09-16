using DiscordCommunityPlugin.UI;
using DiscordCommunityPlugin.UI.FlowCoordinators;
using DiscordCommunityPlugin.UI.ViewControllers;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        public bool playerIsPlayingWithMe = false;

        private MainModFlowCoordinator _mainFlowCooridnator;
        private RectTransform _mainMenuRectTransform;
        private MainMenuViewController _mainMenuViewController;
        private ModalViewController _requiredModsModal;
        private Button _beatSaverButton;

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

                SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
                CreateCommunitiyButton();
            }
        }

        private void SceneManager_activeSceneChanged(Scene prev, Scene next)
        {
            if (next.name == "Menu")
            {
                DiscordCommunityShared.Logger.Warning("CHANGED TO MENU SCENE");
                if (playerIsPlayingWithMe)
                {
                    playerIsPlayingWithMe = false;
                    DiscordCommunityShared.Logger.Warning("SHOULD LOAD MOD MENU");
                    CreateCommunitiyButton();
                }
                else CreateCommunitiyButton();
            }
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

            _beatSaverButton = BaseUI.CreateUIButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                (_beatSaverButton.transform as RectTransform).anchoredPosition = new Vector2(61f, 5f);
                (_beatSaverButton.transform as RectTransform).sizeDelta = new Vector2(38f, 10f);

                BaseUI.SetButtonText(_beatSaverButton, "DiscordCommunity");

                _beatSaverButton.onClick.AddListener(() => {
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
                DiscordCommunityShared.Logger.Error("Error: " + e.Message);
                DiscordCommunityShared.Logger.Error(e.StackTrace);
            }
        }
    }
}