﻿using CustomUI.BeatSaber;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamSaberPlugin.Helpers;
using TeamSaberPlugin.Misc;
using TeamSaberPlugin.UI.ViewControllers;
using TeamSaberShared;
using UnityEngine;
using VRUI;
using static TeamSaberShared.SharedConstructs;
using Logger = TeamSaberShared.Logger;

namespace TeamSaberPlugin.UI.FlowCoordinators
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class MainModFlowCoordinator : FlowCoordinator
    {
        public MainFlowCoordinator mfc;
        public MainMenuViewController mmvc;
        public SongListViewController songListViewController;

        private GeneralNavigationController _mainModNavigationController;
        private AdditionalContentModelSO _additionalContentModel;
        private BeatmapLevelCollectionSO _primaryLevelCollection;
        private BeatmapLevelCollectionSO _secondaryLevelCollection;
        private BeatmapLevelCollectionSO _extrasLevelCollection;
        private SpeedViewController _speedViewController;

        protected PlatformLeaderboardViewController _globalLeaderboard;
        public CustomLeaderboardController _communityLeaderboard; //TEMPORARILY PUBLIC

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

                _speedViewController = BeatSaberUI.CreateViewController<SpeedViewController>();

                ProvideInitialViewControllers(_mainModNavigationController, _communityLeaderboard, _globalLeaderboard, _speedViewController);
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
            if (_additionalContentModel == null)
            {
                _additionalContentModel = Resources.FindObjectsOfTypeAll<AdditionalContentModelSO>().First();
            }
            if (_primaryLevelCollection == null)
            {
                _primaryLevelCollection = _additionalContentModel.alwaysOwnedPacks.First(x => x.packID == OstHelper.packs[0].PackID).beatmapLevelCollection as BeatmapLevelCollectionSO;
            }
            if (_secondaryLevelCollection == null)
            {
                _secondaryLevelCollection = _additionalContentModel.alwaysOwnedPacks.First(x => x.packID == OstHelper.packs[1].PackID).beatmapLevelCollection as BeatmapLevelCollectionSO;
            }
            if (_extrasLevelCollection == null)
            {
                _extrasLevelCollection = _additionalContentModel.alwaysOwnedPacks.First(x => x.packID == OstHelper.packs[2].PackID).beatmapLevelCollection as BeatmapLevelCollectionSO;
            }
            if (_mainModNavigationController.GetField<List<VRUIViewController>>("_viewControllers").IndexOf(songListViewController) < 0)
            {
                SetViewControllersToNavigationConctroller(_mainModNavigationController, new VRUIViewController[] { songListViewController });

                songListViewController.SelectWhenLoaded(songToSelectWhenLoaded);
                songListViewController.SongListRowSelected += SongListRowSelected;
                songListViewController.ReloadPressed += () => ReloadServerData();
                ReloadServerData();
            }
        }

        private void SongListRowSelected(Song song)
        {
            //Open up the custom/global leaderboard pane when we need to
            if (_communityLeaderboard == null)
            {
                _communityLeaderboard = BeatSaberUI.CreateViewController<CustomLeaderboardController>();
                _communityLeaderboard.PlayPressed += SongPlayPressed;
            }
            SetLeftScreenViewController(_communityLeaderboard);

            if (_globalLeaderboard == null)
            {
                _globalLeaderboard = Resources.FindObjectsOfTypeAll<PlatformLeaderboardViewController>().First();
                _globalLeaderboard.name = "Community Global Leaderboard";
            }
            SetRightScreenViewController(_globalLeaderboard);

            //Change global leaderboard view
            _globalLeaderboard.SetData(song.Beatmap);

            //Change community leaderboard view
            //Use the currently selected team, if it exists
            int teamIndex = _communityLeaderboard.selectedTeamIndex;
            if (teamIndex <= -1) teamIndex = Team.allTeams.FindIndex(x => x.TeamId == Player.Instance.team);
            _communityLeaderboard.SetSong(song, teamIndex);
        }

        private void ReloadServerData()
        {
            Client.GetDataForDiscordCommunityPlugin(new BeatmapLevelCollectionSO[] { _primaryLevelCollection, _secondaryLevelCollection, _extrasLevelCollection }, songListViewController, Plugin.PlayerId.ToString());
        }

        //BSUtils: disable gameplay-modifying plugins

        private void BSUtilsDisableOtherPlugins()
        {
            BS_Utils.Gameplay.Gamemode.NextLevelIsIsolated("TeamSaberPlugin");
            Logger.Success("Disabled game-modifying plugins through bs_utils :)");
        }

        private void SongPlayPressed(Song song)
        {
            if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name.ToLower() == "Beat Saber Utils".ToLower()))
            {
                BSUtilsDisableOtherPlugins();
            }
            else Logger.Warning("BSUtils not installed, not disabling other plugins");

            //We're playing from the mod's menu
            CommunityUI.instance.communitySongPlayed = song.Beatmap.level.levelID;

            //Callback for when the song is ready to be played
            Action<IBeatmapLevel> SongLoaded = (loadedLevel) =>
            {
                MenuTransitionsHelperSO menuTransitionHelper = Resources.FindObjectsOfTypeAll<MenuTransitionsHelperSO>().FirstOrDefault();
                var playerSettings = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>()
                    .FirstOrDefault()?
                    .currentLocalPlayer.playerSpecificSettings;

                //Override defaults if we have forced options enabled
                if (song.PlayerOptions != PlayerOptions.None)
                {
                    playerSettings = new PlayerSpecificSettings();
                    playerSettings.leftHanded = song.PlayerOptions.HasFlag(PlayerOptions.Mirror);
                    playerSettings.swapColors = song.PlayerOptions.HasFlag(PlayerOptions.Mirror);
                    playerSettings.staticLights = song.PlayerOptions.HasFlag(PlayerOptions.StaticLights);
                    playerSettings.noTextsAndHuds = song.PlayerOptions.HasFlag(PlayerOptions.NoHud);
                    playerSettings.advancedHud = song.PlayerOptions.HasFlag(PlayerOptions.AdvancedHud);
                    playerSettings.reduceDebris = song.PlayerOptions.HasFlag(PlayerOptions.ReduceDebris);
                }

                GameplayModifiers gameplayModifiers = new GameplayModifiers();
                gameplayModifiers.noFail = song.GameOptions.HasFlag(GameOptions.NoFail);
                //gameplayModifiers.noArrows = song.GameOptions.HasFlag(GameOptions.NoArrows);
                gameplayModifiers.noBombs = song.GameOptions.HasFlag(GameOptions.NoBombs);
                gameplayModifiers.noObstacles = song.GameOptions.HasFlag(GameOptions.NoObstacles);
                //gameplayModifiers.noWalls = song.GameOptions.HasFlag(GameOptions.NoWalls);
                if (song.GameOptions.HasFlag(GameOptions.SlowSong))
                {
                    gameplayModifiers.songSpeed = GameplayModifiers.SongSpeed.Slower;
                }
                else if (song.GameOptions.HasFlag(GameOptions.FastSong))
                {
                    gameplayModifiers.songSpeed = GameplayModifiers.SongSpeed.Faster;
                }

                gameplayModifiers.instaFail = song.GameOptions.HasFlag(GameOptions.InstaFail);
                gameplayModifiers.failOnSaberClash = song.GameOptions.HasFlag(GameOptions.FailOnClash);
                gameplayModifiers.batteryEnergy = song.GameOptions.HasFlag(GameOptions.BatteryEnergy);
                gameplayModifiers.fastNotes = song.GameOptions.HasFlag(GameOptions.FastNotes);
                gameplayModifiers.disappearingArrows = song.GameOptions.HasFlag(GameOptions.DisappearingArrows);
                gameplayModifiers.ghostNotes = song.GameOptions.HasFlag(GameOptions.GhostNotes);

                var practiceSettings = new PracticeSettings();
                practiceSettings.songSpeedMul = Config.Speed;
                song.Speed = Config.Speed;

                menuTransitionHelper.StartStandardLevel(song.Beatmap, gameplayModifiers, playerSettings, practiceSettings, null, SongFinished);
            };

            //Load audio if it's custom
            if (song.Beatmap.level is CustomLevel)
            {
                SongLoader.Instance.LoadAudioClipForLevel((CustomLevel)song.Beatmap.level, SongLoaded);
            }
            else
            {
                SongLoaded(song.Beatmap.level);
            }
        }

        private bool BSUtilsScoreDisabled()
        {
            return BS_Utils.Gameplay.ScoreSubmission.Disabled || BS_Utils.Gameplay.ScoreSubmission.ProlongedDisabled;
        }

        private void SongFinished(StandardLevelScenesTransitionSetupDataSO standardLevelScenesTransitionSetupData, LevelCompletionResults results)
        {
            standardLevelScenesTransitionSetupData.didFinishEvent -= SongFinished;

            if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Restart)
            {
                var song = _communityLeaderboard.selectedSong;
                MenuTransitionsHelperSO menuTransitionHelper = Resources.FindObjectsOfTypeAll<MenuTransitionsHelperSO>().FirstOrDefault();
                var playerSettings = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>()
                    .FirstOrDefault()?
                    .currentLocalPlayer.playerSpecificSettings;

                //Override defaults if we have forced options enabled
                if (song.PlayerOptions != PlayerOptions.None)
                {
                    playerSettings = new PlayerSpecificSettings();
                    playerSettings.leftHanded = song.PlayerOptions.HasFlag(PlayerOptions.Mirror);
                    playerSettings.swapColors = song.PlayerOptions.HasFlag(PlayerOptions.Mirror);
                    playerSettings.staticLights = song.PlayerOptions.HasFlag(PlayerOptions.StaticLights);
                    playerSettings.noTextsAndHuds = song.PlayerOptions.HasFlag(PlayerOptions.NoHud);
                    playerSettings.advancedHud = song.PlayerOptions.HasFlag(PlayerOptions.AdvancedHud);
                    playerSettings.reduceDebris = song.PlayerOptions.HasFlag(PlayerOptions.ReduceDebris);
                }

                menuTransitionHelper.StartStandardLevel(
                    song.Beatmap,
                    results.gameplayModifiers,
                    playerSettings,
                    null,
                    null,
                    SongFinished
                );
            }
            else if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared) //Didn't quit and didn't die
            {
                //If bs_utils disables score submission, we do too
                if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name.ToLower() == "Beat Saber Utils".ToLower()))
                {
                    if (BSUtilsScoreDisabled()) return;
                }

                IBeatmapLevel level = _communityLeaderboard.selectedSong.Beatmap.level;
                string songHash = level.levelID.Substring(0, Math.Min(32, level.levelID.Length));
                string songId = null;

                //If the song is an OST, just send the hash
                if (OstHelper.IsOst(songHash))
                {
                    songId = songHash;
                }
                else
                {
                    songId = SongIdHelper.GetSongIdFromLevelId(level.levelID);
                }

                //Community leaderboards
                var sn = _communityLeaderboard.GetField("selectedSong");
                var po = sn.GetProperty("PlayerOptions");
                var go = sn.GetProperty("GameOptions");
                var ss = sn.GetProperty("Speed");

                var d = sn.GetProperty("Beatmap");
                var rs = results.GetProperty("unmodifiedScore");
                var fc = results.GetProperty("fullCombo");
                var ms = typeof(RSA);

                var s = ms.InvokeMethod("SignScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), fc, rs, (int)po, (int)go, (int)(((float)ss) * 100));

                var c = typeof(Client);
                Action<bool> don = (b) =>
                {
                    //TODO: Fix refresh freeze issue
                    //if (b && _communityLeaderboard) _communityLeaderboard.Refresh();
                };
                var cs = c.InvokeMethod("SubmitScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), fc, rs, s, (int)po, (int)go, (int)(((float)ss) * 100), don);

                //Scoresaber leaderboards
                /*
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
                var dbm = _communityLeaderboard.GetField("selectedSong").GetProperty("Beatmap");
                var gm = results.GetProperty("gameplayModifiers");
                var lest = results.GetProperty("levelEndStateType");
                var cld = (int)lest == (int)LevelCompletionResults.LevelEndStateType.Cleared;
                var lid = dbm.GetProperty("level").GetProperty("levelID");
                var dif = dbm.GetProperty("difficulty");
                var plsd = clp.InvokeMethod("GetPlayerLevelStatsData", lid, dif, dbm.GetProperty("parentDifficultyBeatmapSet").GetProperty("beatmapCharacteristic"));
                var res = (int)plsd.GetProperty("highScore") < (int)results.GetProperty("score");
                plsd.InvokeMethod("IncreaseNumberOfGameplays");

                if (cld && res)
                {
                    plsd.InvokeMethod("UpdateScoreData", results.GetProperty("score"), results.GetProperty("maxCombo"), results.GetProperty("fullCombo"), results.GetProperty("rank"));
                    plm.InvokeMethod("AddScore", dbm, results.GetProperty("unmodifiedScore"), gm);
                }
                */

                //var song = _communityLeaderboard.selectedSong;
                //string signed = RSA.SignScore(Plugin.PlayerId, songId, (int)_communityLeaderboard.selectedSong.Beatmap.difficulty, results.fullCombo, results.unmodifiedScore, (int)song.PlayerOptions, (int)song.GameOptions, (int)(song.Speed * 100));
                //Client.SubmitScore(Plugin.PlayerId, songId, (int)_communityLeaderboard.selectedSong.Beatmap.difficulty, results.fullCombo, results.unmodifiedScore, signed, (int)song.PlayerOptions, (int)song.GameOptions, (int)(song.Speed * 100));  

                /*
                var platformLeaderboardsModel = Resources.FindObjectsOfTypeAll<PlatformLeaderboardsModel>().First();
                var playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
                playerDataModel.currentLocalPlayer.playerAllOverallStatsData.soloFreePlayOverallStatsData.UpdateWithLevelCompletionResults(results);
                playerDataModel.Save();

                PlayerDataModelSO.LocalPlayer currentLocalPlayer = playerDataModel.currentLocalPlayer;
                IDifficultyBeatmap difficultyBeatmap = _communityLeaderboard.selectedSong.Beatmap;
                GameplayModifiers gameplayModifiers = results.gameplayModifiers;
                bool cleared = results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared;
                string levelID = difficultyBeatmap.level.levelID;
                BeatmapDifficulty difficulty = difficultyBeatmap.difficulty;
                PlayerLevelStatsData playerLevelStatsData = currentLocalPlayer.GetPlayerLevelStatsData(levelID, difficulty, difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
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
    }
}