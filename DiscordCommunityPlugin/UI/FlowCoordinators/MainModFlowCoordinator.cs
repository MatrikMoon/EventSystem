using CustomUI.BeatSaber;
using DiscordCommunityPlugin.DiscordCommunityHelpers;
using DiscordCommunityPlugin.Misc;
using DiscordCommunityPlugin.UI.ViewControllers;
using HMUI;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRUI;
using static DiscordCommunityShared.SharedConstructs;
using Logger = DiscordCommunityShared.Logger;

namespace DiscordCommunityPlugin.UI.FlowCoordinators
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class MainModFlowCoordinator : FlowCoordinator
    {
        public MainFlowCoordinator mfc;
        public MainMenuViewController mmvc;
        public SongListViewController songListViewController;

        private GeneralNavigationController _mainModNavigationController;
        private LevelCollectionSO _levelCollections;

        protected PlatformLeaderboardViewController _globalLeaderboard;
        protected CustomLeaderboardController _communityLeaderboard;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (activationType == ActivationType.AddedToHierarchy)
            {
                //TODO: The following is a potential memory leak...????
                //If the navigation controller has previously been dismissd, it will cause
                //an error if something tries to dismiss it again
                _mainModNavigationController = BeatSaberUI.CreateViewController<GeneralNavigationController>();
                _mainModNavigationController.didFinishEvent += (_) => mfc.InvokeMethod("DismissFlowCoordinator", this, null, false);

                ProvideInitialViewControllers(_mainModNavigationController, null, null);
                OpenSongsList();
            }
        }

        public void PresentMainModUI()
        {
            mfc.InvokeMethod("PresentFlowCoordinatorOrAskForTutorial", this);
        }

        public void OpenSongsList(string songToSelectWhenLoaded = null)
        {
            if (songListViewController == null)
            {
                songListViewController = BeatSaberUI.CreateViewController<SongListViewController>();
            }
            if (_levelCollections == null)
            {
                _levelCollections = Resources.FindObjectsOfTypeAll<LevelCollectionSO>().First();
            }
            if (_mainModNavigationController.GetField<List<VRUIViewController>>("_viewControllers").IndexOf(songListViewController) < 0)
            {
                SetViewControllersToNavigationConctroller(_mainModNavigationController, new VRUIViewController[] { songListViewController });

                songListViewController.SelectWhenLoaded(songToSelectWhenLoaded);
                songListViewController.SongListRowSelected += SongListRowSelected;
                songListViewController.ReloadPressed += () =>
                {
                    mfc.InvokeMethod("DismissFlowCoordinator", this, null, false);
                };
                ReloadServerData();
            }
        }

        private void SongListRowSelected(IBeatmapLevel level)
        {
            //Open up the custom/global leaderboard pane when we need to
            if (_communityLeaderboard == null)
            {
                _communityLeaderboard = BeatSaberUI.CreateViewController<CustomLeaderboardController>();
                _communityLeaderboard.RequestRankPressed += () => mfc.InvokeMethod("DismissFlowCoordinator", this, null, false);
                _communityLeaderboard.PlayPressed += SongPlayPressed;
                SetLeftScreenViewController(_communityLeaderboard);
            }
            if (_globalLeaderboard == null)
            {
                _globalLeaderboard = Resources.FindObjectsOfTypeAll<PlatformLeaderboardViewController>().First();
                _globalLeaderboard.name = "Community Global Leaderboard";
                SetRightScreenViewController(_globalLeaderboard);
            }

            //Change global leaderboard view
            IDifficultyBeatmap difficultyLevel = Player.Instance.GetMapForRank(level);
            _globalLeaderboard.SetData(difficultyLevel);

            //Change community leaderboard view
            //Use the currently selected rank, if it exists
            Rank rankToView = _communityLeaderboard.selectedRank;
            if (rankToView <= Rank.None) rankToView = Player.Instance.rank;
            _communityLeaderboard.SetSong(difficultyLevel, rankToView);
        }

        private void ReloadServerData()
        {
            Client.GetDataForDiscordCommunityPlugin(_levelCollections, songListViewController, Plugin.PlayerId.ToString());
        }

        private void SongPlayPressed(IDifficultyBeatmap map)
        {
            //We're playing from the mod's menu
            CommunityUI.instance.communitySongPlayed = map.level.levelID;

            //Callback for when the song is ready to be played
            Action<IBeatmapLevel> SongLoaded = (loadedLevel) =>
            {
                MenuSceneSetupDataSO _menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupDataSO>().FirstOrDefault();
                PlayerSpecificSettings playerSettings = new PlayerSpecificSettings();
                playerSettings.leftHanded = Config.MirrorMode;
                playerSettings.staticLights = Config.StaticLights;
                GameplayModifiers gameplayModifiers = new GameplayModifiers();
                _menuSceneSetupData.StartStandardLevel(map, gameplayModifiers, playerSettings, null, null, SongFinished);
            };

            //Load audio if it's custom
            if (map.level is CustomLevel)
            {
                SongLoader.Instance.LoadAudioClipForLevel((CustomLevel)map.level, SongLoaded);
            }
            else
            {
                SongLoaded(map.level);
            }
        }

        private void SongFinished(StandardLevelSceneSetupDataSO standardLevelSceneSetupData, LevelCompletionResults results)
        {
            //Doesn't seem to be needed with the current method of song start
            //standardLevelSceneSetupData.PopScenes((results.levelEndStateType != LevelCompletionResults.LevelEndStateType.Failed && results.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared) ? 0.35f : 1.3f);

            standardLevelSceneSetupData.didFinishEvent -= SongFinished;

            try
            {
                if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared) //Didn't quit and didn't die
                {
                    IBeatmapLevel level = standardLevelSceneSetupData.difficultyBeatmap.level;
                    string songHash = level.levelID.Substring(0, Math.Min(32, level.levelID.Length));
                    string songId = null;

                    //If the song is an OST, just send the hash
                    if (DiscordCommunityShared.OstHelper.IsOst(songHash))
                    {
                        songId = songHash;
                    }
                    else
                    {
                        songId = SongIdHelper.GetSongIdFromLevelId(level.levelID);
                    }

                    string signed = DiscordCommunityShared.RSA.SignScore(Plugin.PlayerId, songId, (int)standardLevelSceneSetupData.difficultyBeatmap.difficulty, results.fullCombo, results.unmodifiedScore);
                    Client.SubmitScore(Plugin.PlayerId, songId, (int)standardLevelSceneSetupData.difficultyBeatmap.difficulty, results.fullCombo, results.unmodifiedScore, signed);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"SongFinished error: {e}");
            }
        }
    }
}