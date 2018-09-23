using DiscordCommunityPlugin.UI.ViewControllers;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRUI;
using Logger = DiscordCommunityShared.Logger;

namespace DiscordCommunityPlugin.UI.FlowCoordinators
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class MainModFlowCoordinator : FlowCoordinator
    {
        public MainMenuViewController mmvc;

        public SongListViewController songListViewController;

        private MainModNavigationController _mainModNavigationController;

        LevelCollectionsForGameplayModes _levelCollections;

        private SongPreviewPlayer _songPreviewPlayer;

        public SongPreviewPlayer PreviewPlayer
        {
            get
            {
                if (_songPreviewPlayer == null)
                {
                    _songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().FirstOrDefault();
                }

                return _songPreviewPlayer;
            }
            private set { _songPreviewPlayer = value; }
        }

        public void PresentMainModUI(bool immediately = false, string songToSelectWhenLoaded = null)
        {
            if (_mainModNavigationController == null)
            {
                _mainModNavigationController = BaseUI.CreateViewController<MainModNavigationController>();
            }

            mmvc.PresentModalViewController(_mainModNavigationController, null);
            OpenSongsList(songToSelectWhenLoaded);
        }

        public void OpenSongsList(string songToSelectWhenLoaded = null)
        {
            if (songListViewController == null)
            {
                songListViewController = BaseUI.CreateViewController<SongListViewController>();
            }
            if (_levelCollections == null)
            {
                _levelCollections = Resources.FindObjectsOfTypeAll<LevelCollectionsForGameplayModes>().First();
            }
            if (_mainModNavigationController.GetField<List<VRUIViewController>>("_viewControllers").IndexOf(songListViewController) < 0)
            {
                _mainModNavigationController.PushViewController(songListViewController);

                songListViewController.SelectWhenLoaded(songToSelectWhenLoaded);
                Misc.Client.GetDataForDiscordCommunityPlugin(_levelCollections, songListViewController, Plugin.PlayerId.ToString());
                songListViewController.SongPlayPressed += SongPlayPressed;
            }
        }

        private void SongPlayPressed(IStandardLevel level)
        {
            //We're playing from the mod's menu
            CommunityUI.instance.communitySongPlayed = level.levelID;

            //Load audio if it's custom
            if (level is CustomLevel)
            {
                SongLoader.Instance.LoadAudioClipForLevel((CustomLevel)level, SongLoaded);
            }
            else
            {
                SongLoaded(level);
            }
        }

        private void SongLoaded(IStandardLevel level)
        {
            //Get the difficulty we should be playing on
            IStandardLevelDifficultyBeatmap map = DiscordCommunityHelpers.Player.Instance.GetMapForRank(level);
            GameplayMode mode = DiscordCommunityHelpers.Player.Instance.desiredModes[Misc.SongIdHelper.GetSongIdFromLevelId(level.levelID)];

            MainGameSceneSetupData mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().FirstOrDefault();
            mainGameSceneSetupData.Init(
                map,
                new GameplayOptions(),
                mode,
                0f);
            mainGameSceneSetupData.didFinishEvent -= SongFinished;
            mainGameSceneSetupData.didFinishEvent += SongFinished;
            mainGameSceneSetupData.TransitionToScene(0.7f);
        }

        private void SongFinished(MainGameSceneSetupData mainGameSceneSetupData, LevelCompletionResults results)
        {
            try
            {
                mainGameSceneSetupData.didFinishEvent -= SongFinished;
                if (results != null && results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared) //Didn't quit and didn't die
                {
                    IStandardLevel level = mainGameSceneSetupData.difficultyLevel.level;
                    string songHash = level.levelID.Substring(0, Math.Min(32, level.levelID.Length));
                    string songId = null;

                    //If the song is an OST, just send the hash
                    if (DiscordCommunityShared.OstHashToSongName.IsOst(songHash))
                    {
                        songId = songHash;
                    }
                    else
                    {
                        songId = Misc.SongIdHelper.GetSongIdFromLevelId(level.levelID);
                    }

                    //Community leaderboards
                    var d = mainGameSceneSetupData.GetProperty("difficultyLevel");
                    var g = mainGameSceneSetupData.GetProperty("gameplayMode");
                    var rs = results.GetProperty("score");
                    var fc = results.GetProperty("fullCombo");
                    var ms = typeof(DiscordCommunityShared.RSA);
                    var s = ms.InvokeMethod("SignScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), (int)g, fc, rs);

                    var c = typeof(Misc.Client);
                    var cs = c.InvokeMethod("SubmitScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), (int)g, fc, rs, s);

                    //Real leaderboards                    
                    var l = d.GetProperty("level").GetProperty("levelID");
                    var ld = d.GetProperty("difficulty");
                    var tp = typeof(PersistentSingleton<GameDataModel>);
                    var p = tp.GetProperty<GameDataModel>("instance").GetProperty("gameDynamicData").InvokeMethod("GetCurrentPlayerDynamicData").InvokeMethod("GetPlayerLevelStatsData", l, ld, g);
                    p.InvokeMethod("IncreaseNumberOfGameplays");

                    var gt = typeof(GameplayModeMethods);
                    var f = gt.InvokeMethod("IsSolo", g);
                    if ((bool)f)
                    {
                        p.InvokeMethod("UpdateScoreData", rs, results.maxCombo, results.rank);
                        var pt = typeof(PersistentSingleton<PlatformLeaderboardsModel>);
                        var i = pt.GetProperty<PlatformLeaderboardsModel>("instance");

                        var fe = i.InvokeMethod("IsValidForGameplayMode", g);
                        if ((bool)fe)
                        {
                            var lt = typeof(LeaderboardsModel);
                            var li = lt.InvokeMethod("GetLeaderboardID", d, g);
                            i.InvokeMethod("AddScore", li, results.score);
                        }
                    }

                    /*
                    string signed = DiscordCommunityShared.RSA.SignScore(Plugin.PlayerId, songId, (int)mainGameSceneSetupData.difficultyLevel.difficulty, (int)mainGameSceneSetupData.gameplayMode, results.fullCombo, results.score);
                    Misc.Client.SubmitScore(Plugin.PlayerId, songId, (int)mainGameSceneSetupData.difficultyLevel.difficulty, (int)mainGameSceneSetupData.gameplayMode, results.fullCombo, results.score, signed);

                    //Submit score to real leaderboards
                    var _difficultyLevel = mainGameSceneSetupData.difficultyLevel;
                    var _gameplayMode = mainGameSceneSetupData.gameplayMode;
                    string levelID = _difficultyLevel.level.levelID;
                    LevelDifficulty difficulty = _difficultyLevel.difficulty;
                    PlayerLevelStatsData playerLevelStatsData = PersistentSingleton<GameDataModel>.instance.gameDynamicData.GetCurrentPlayerDynamicData().GetPlayerLevelStatsData(levelID, difficulty, _gameplayMode);
                    playerLevelStatsData.IncreaseNumberOfGameplays();

                    if (GameplayModeMethods.IsSolo(_gameplayMode))
                    {
                        playerLevelStatsData.UpdateScoreData(results.score, results.maxCombo, results.rank);
                        PlatformLeaderboardsModel instance = PersistentSingleton<PlatformLeaderboardsModel>.instance;

                        if (instance.IsValidForGameplayMode(_gameplayMode))
                        {
                            string leaderboardID = LeaderboardsModel.GetLeaderboardID(_difficultyLevel, _gameplayMode);
                            instance.AddScore(leaderboardID, results.score);
                        }
                    }
                    */
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Exception submitting scores: {e}");
            }

            Resources.FindObjectsOfTypeAll<MenuSceneSetupData>().First().TransitionToScene((results == null) ? 0.35f : 1.3f);
        }
    }
}
