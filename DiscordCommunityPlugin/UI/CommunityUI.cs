using DiscordCommunityPlugin.UI;
using DiscordCommunityPlugin.UI.FlowCoordinators;
using DiscordCommunityPlugin.UI.ViewControllers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/**
 * Created by Moon on 8/23/2018
 * Serves as the main UI class for the Plugin
 * Heavily influenced by BeatSaverDownloader
 * (https://github.com/andruzzzhka/BeatSaverDownloader/)
 */

namespace DiscordCommunityPlugin
{
    class CommunityUI : MonoBehaviour
    {
        public static CommunityUI _instance;

        private RectTransform _mainMenuRectTransform;
        private MainMenuViewController _mainMenuViewController;
        private ModalViewController _requiredModsModal;
        private Button _beatSaverButton;
        public MainModFlowCoordinator _mainFlowCooridnator;
        
        public static void OnLoad()
        {
            if (_instance != null)
            {
                return;
            }
            new GameObject("Discord Community Plugin").AddComponent<CommunityUI>();
        }

        public void Awake()
        {
            _instance = this;
        }

        public void Start()
        {
            _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
            _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;
            if (_mainFlowCooridnator == null)
            {
                _mainFlowCooridnator = new GameObject("MainModFlow").AddComponent<MainModFlowCoordinator>();
                _mainFlowCooridnator.mmvc = _mainMenuViewController;
            }
            CreateCommunitiyButton();
        }

        private void CreateCommunitiyButton()
        {
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
                        Logger.Warning("TRYING TO LAUNCH MAIN MOD FLOW");
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
    }
}