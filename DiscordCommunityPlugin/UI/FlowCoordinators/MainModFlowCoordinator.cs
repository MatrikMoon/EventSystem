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
        private SimpleDialogPromptViewController _rankUpViewController;

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

                if (_rankUpViewController == null)
                {
                    _rankUpViewController = Instantiate(Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().First());
                }
                _rankUpViewController.didFinishEvent += HandleRankPromptViewControllerDidFinish;

                ProvideInitialViewControllers(_mainModNavigationController, _communityLeaderboard, _globalLeaderboard);
                OpenSongsList();
            }
        }

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            if (deactivationType == DeactivationType.RemovedFromHierarchy)
            {
                _rankUpViewController.didFinishEvent -= HandleRankPromptViewControllerDidFinish;
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
                _communityLeaderboard.PlayPressed += SongPlayPressed;
                _communityLeaderboard.RequestRankPressed += () =>
                {
                    SetLeftScreenViewController(null);
                    SetRightScreenViewController(null);

                    if (Player.Instance.CanRankUp())
                    {
                        if (Player.Instance.rank < Rank.Gold)
                        {
                            var message =
                                $"You are about to rank up from {Player.Instance.rank} to {Player.Instance.rank + 1}.\n" +
                                "Are you sure you want to perform this action?\n" +
                                "You are on the honor system for now.";
                            _rankUpViewController.Init("Rank Up", message, "Yes", "No");
                            PresentViewController(_rankUpViewController);
                        }
                        else if (Player.Instance.rank == Rank.Gold)
                        {
                            var message =
                                "You are about to spend 3 tokens and apply to rank up to Blue.\n" +
                                "Your Scoresaber profile will be submitted to the Blues, where it will then be voted on.\n" +
                                "Are you sure you want to perform this action?";
                            _rankUpViewController.Init("Rank Up", message, "Yes", "No");
                            PresentViewController(_rankUpViewController);
                        }
                    }
                    else if (Player.Instance.rank >= Rank.Gold && Player.Instance.tokens < 3)
                    {
                        var message =
                            "You do not have enough tokens to rank up.\n" +
                            "3 tokens are required.";
                        _rankUpViewController.Init("Rank Up", message, "Ok");
                        PresentViewController(_rankUpViewController);
                    }
                    else
                    {
                        var message = "You must improve your scores on the following songs\n" +
                            "(check #rules-and-info for the difficulty you need to play on):\n";
                        Player.Instance.GetSongsToImproveBeforeRankUp().ForEach(x => message += $"{DiscordCommunityShared.OstHelper.GetOstSongNameFromLevelId(x)}\n");
                        _rankUpViewController.Init("Rank Up", message, "Ok");
                        PresentViewController(_rankUpViewController);
                    }
                };
            }
            SetLeftScreenViewController(_communityLeaderboard);

            if (_globalLeaderboard == null)
            {
                _globalLeaderboard = Resources.FindObjectsOfTypeAll<PlatformLeaderboardViewController>().First();
                _globalLeaderboard.name = "Community Global Leaderboard";
            }
            SetRightScreenViewController(_globalLeaderboard);

            //Change global leaderboard view
            IDifficultyBeatmap difficultyLevel = Player.Instance.GetMapForRank(level);
            _globalLeaderboard.SetData(difficultyLevel);

            //Change community leaderboard view
            //Use the currently selected rank, if it exists
            Rank rankToView = _communityLeaderboard.selectedRank;
            if (rankToView <= Rank.None) rankToView = Player.Instance.rank;
            _communityLeaderboard.SetSong(difficultyLevel, rankToView);
        }

        public virtual void HandleRankPromptViewControllerDidFinish(SimpleDialogPromptViewController viewController, bool ok)
        {
            if (ok && Player.Instance.CanRankUp())
            {
                DismissViewController(viewController, immediately: true);
                mfc.InvokeMethod("DismissFlowCoordinator", this, null, false);
                string signed = DiscordCommunityShared.RSA.SignRankRequest(Plugin.PlayerId, Player.Instance.rank + 1, false);
                Client.RequestRank(Plugin.PlayerId, Player.Instance.rank + 1, false, signed);
            }
            else DismissViewController(viewController);
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
            standardLevelSceneSetupData.didFinishEvent -= SongFinished;

            try
            {
                if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Restart)
                {
                    SongPlayPressed(standardLevelSceneSetupData.difficultyBeatmap);
                }
                else if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared) //Didn't quit and didn't die
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

                    //Community leaderboards
                    var d = standardLevelSceneSetupData.GetProperty("difficultyBeatmap");
                    var rs = results.GetProperty("unmodifiedScore");
                    var fc = results.GetProperty("fullCombo");
                    var ms = typeof(DiscordCommunityShared.RSA);
                    var s = ms.InvokeMethod("SignScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), fc, rs);

                    var c = typeof(Client);
                    Action<bool> don = (b) =>
                    {
                        if (b && _communityLeaderboard) _communityLeaderboard.Refresh();
                    };
                    var cs = c.InvokeMethod("SubmitScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), fc, rs, s, don);

                    //Scoresaber leaderboards
                    var plmt = ReflectionUtil.GetStaticType("PlatformLeaderboardsModel, Assembly-CSharp");
                    var pdmt = ReflectionUtil.GetStaticType("PlayerDataModelSO, Assembly-CSharp");
                    var plm = Resources.FindObjectsOfTypeAll(plmt).First();
                    var pdm = Resources.FindObjectsOfTypeAll(pdmt).First();
                    pdm.GetProperty("currentLocalPlayer")
                        .GetProperty("playerAllOverallStatsData")
                        .GetProperty("soloFreePlayOverallStatsData")
                        .InvokeMethod("UpdateWithLevelCompletionResults", results);
                    pdm.InvokeMethod("Save");

                    var clp = pdm.GetProperty("currentLocalPlayer");
                    var dbm = standardLevelSceneSetupData.GetProperty("difficultyBeatmap");
                    var gm = standardLevelSceneSetupData
                        .GetProperty("gameplayCoreSetupData")
                        .GetProperty("gameplayModifiers");
                    var lest = results.GetProperty("levelEndStateType");
                    var cld = (int)lest == (int)LevelCompletionResults.LevelEndStateType.Cleared;
                    var lid = dbm.GetProperty("level").GetProperty("levelID");
                    var dif = dbm.GetProperty("difficulty");
                    var plsd = clp.InvokeMethod("GetPlayerLevelStatsData", lid, dif);
                    var res = (int)plsd.GetProperty("highScore") < (int)results.GetProperty("score");
                    plsd.InvokeMethod("IncreaseNumberOfGameplays");
                    
                    if (cld && res)
                    {
                        plsd.InvokeMethod("UpdateScoreData", results.GetProperty("score"), results.GetProperty("maxCombo"), results.GetProperty("fullCombo"), results.GetProperty("rank"));
                        plm.InvokeMethod("AddScore", dbm, results.GetProperty("unmodifiedScore"), gm);
                    }

                    //string signed = DiscordCommunityShared.RSA.SignScore(Plugin.PlayerId, songId, (int)standardLevelSceneSetupData.difficultyBeatmap.difficulty, results.fullCombo, results.unmodifiedScore);
                    //Client.SubmitScore(Plugin.PlayerId, songId, (int)standardLevelSceneSetupData.difficultyBeatmap.difficulty, results.fullCombo, results.unmodifiedScore, signed);

                    /*
                    var platformLeaderboardsModel = Resources.FindObjectsOfTypeAll<PlatformLeaderboardsModel>().First();
                    var playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
                    playerDataModel.currentLocalPlayer.playerAllOverallStatsData.soloFreePlayOverallStatsData.UpdateWithLevelCompletionResults(results);
                    playerDataModel.Save();

                    PlayerDataModelSO.LocalPlayer currentLocalPlayer = playerDataModel.currentLocalPlayer;
                    IDifficultyBeatmap difficultyBeatmap = standardLevelSceneSetupData.difficultyBeatmap;
                    GameplayModifiers gameplayModifiers = standardLevelSceneSetupData.gameplayCoreSetupData.gameplayModifiers;
                    bool cleared = results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared;
                    string levelID = difficultyBeatmap.level.levelID;
                    BeatmapDifficulty difficulty = difficultyBeatmap.difficulty;
                    PlayerLevelStatsData playerLevelStatsData = currentLocalPlayer.GetPlayerLevelStatsData(levelID, difficulty);
                    bool result = playerLevelStatsData.highScore < results.score;
                    playerLevelStatsData.IncreaseNumberOfGameplays();
                    if (cleared && result)
                    {
                        playerLevelStatsData.UpdateScoreData(results.score, results.maxCombo, results.fullCombo, results.rank);
                        platformLeaderboardsModel.AddScore(difficultyBeatmap, results.unmodifiedScore, gameplayModifiers);
                    }
                    */
                }
            }
            catch (Exception e)
            {
                Logger.Error($"SongFinished error: {e}");
            }
        }
    }
}