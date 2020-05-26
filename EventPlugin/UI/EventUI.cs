using BeatSaberMarkupLanguage.MenuButtons;
using SongCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventPlugin.UI.FlowCoordinators;
using UnityEngine;
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
        private static MenuButton _communityButton;

        public static void CommunityButtonPressed()
        {
            var mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            var mainModFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<MainModFlowCoordinator>(mainFlowCoordinator.gameObject);
            mainModFlowCoordinator.mainFlowCoordinator = mainFlowCoordinator;
            mainModFlowCoordinator.PresentMainModUI();
        }

        private static void SongsLoaded(Loader _, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            if (_communityButton != null) _communityButton.Interactable = true;
        }

        public static void CreateCommunitiyButton()
        {
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
#elif TRUEACCURACY
                    var buttonName = "True Accuracy Tournament";
                    var hint = "Show off your skills!";
#elif ASIAVR
                    var buttonName = "AsiaVR Tournament";
                    var hint = "Compete in the Asia Server Event!";
#elif BTH
                    var buttonName = "BTH Qualifiers";
                    var hint = "Qualifier plugin for Beat the Hub!";
#else
                    var buttonName = "EVENT BETA";
                    var hint = "STILL A BETA";
#endif
                    _communityButton = new MenuButton(buttonName, hint, CommunityButtonPressed);
                    _communityButton.Interactable = Loader.AreSongsLoaded;
                    MenuButtons.instance.RegisterButton(_communityButton);

                    Loader.SongsLoadedEvent += SongsLoaded;
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
