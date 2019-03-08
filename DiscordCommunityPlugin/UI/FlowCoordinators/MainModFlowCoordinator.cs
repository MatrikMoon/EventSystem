using CustomUI.BeatSaber;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamSaberPlugin.DiscordCommunityHelpers;
using TeamSaberPlugin.Misc;
using TeamSaberPlugin.UI.ViewControllers;
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
        private LevelCollectionSO _levelCollection;

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

                ProvideInitialViewControllers(_mainModNavigationController, _communityLeaderboard, _globalLeaderboard);
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
            if (_levelCollection == null)
            {
                _levelCollection = Resources.FindObjectsOfTypeAll<LevelCollectionSO>().First();
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
            Client.GetDataForDiscordCommunityPlugin(_levelCollection, songListViewController, Plugin.PlayerId.ToString());
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
                //If one-saber is enabled, we will forcefully add it to the level characteristics
                //We need to grab the "characteristic" and use it for ourselves
                if (song.GameOptions.HasFlag(GameOptions.OneSaber))
                {
                    BeatmapCharacteristicSO oneSaberCharacteristic = null;
                    for (int i = 0; oneSaberCharacteristic == null && i < _levelCollection.levels.Length; i++)
                    {
                        oneSaberCharacteristic = _levelCollection.levels.ElementAt(i).beatmapCharacteristics.FirstOrDefault(x => x.characteristicName == "One Saber");
                    }

                    //If we didn't end up finding it, just skip it
                    if (oneSaberCharacteristic != null)
                    {
                        List<BeatmapCharacteristicSO> newCharacteristics = new List<BeatmapCharacteristicSO>();
                        newCharacteristics.AddRange(loadedLevel.beatmapCharacteristics);
                        newCharacteristics.Add(oneSaberCharacteristic);
                        loadedLevel.SetField("_beatmapCharacteristics", newCharacteristics.ToArray());
                    }
                }

                MenuSceneSetupDataSO _menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupDataSO>().FirstOrDefault();
                var playerSettings = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>()
                    .FirstOrDefault()?
                    .currentLocalPlayer.playerSpecificSettings;

                playerSettings.leftHanded = song.PlayerOptions.HasFlag(PlayerOptions.Mirror);
                playerSettings.swapColors = song.PlayerOptions.HasFlag(PlayerOptions.Mirror);
                playerSettings.staticLights = song.PlayerOptions.HasFlag(PlayerOptions.StaticLights);
                playerSettings.noTextsAndHuds = song.PlayerOptions.HasFlag(PlayerOptions.NoHud);
                playerSettings.advancedHud = song.PlayerOptions.HasFlag(PlayerOptions.AdvancedHud);
                playerSettings.reduceDebris = song.PlayerOptions.HasFlag(PlayerOptions.ReduceDebris);

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

                _menuSceneSetupData.StartStandardLevel(song.Beatmap, gameplayModifiers, playerSettings, null, null, SongFinished);
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
                    //If bs_utils disables score submission, we do too
                    if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name.ToLower() == "Beat Saber Utils".ToLower()))
                    {
                        if (BSUtilsScoreDisabled()) return;
                    }

                    IBeatmapLevel level = standardLevelSceneSetupData.difficultyBeatmap.level;
                    string songHash = level.levelID.Substring(0, Math.Min(32, level.levelID.Length));
                    string songId = null;

                    //If the song is an OST, just send the hash
                    if (TeamSaberShared.OstHelper.IsOst(songHash))
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
                    var ms = typeof(TeamSaberShared.RSA);
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

                    //string signed = TeamSaberShared.RSA.SignScore(Plugin.PlayerId, songId, (int)standardLevelSceneSetupData.difficultyBeatmap.difficulty, results.fullCombo, results.unmodifiedScore);
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