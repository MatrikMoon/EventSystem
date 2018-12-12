using ChristmasVotePlugin.Misc;
using ChristmasVotePlugin.UI.ViewControllers;
using CustomUI.BeatSaber;
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

namespace ChristmasVotePlugin.UI.FlowCoordinators
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
        protected CustomLeftViewController _communityLeaderboard;

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
                _communityLeaderboard = BeatSaberUI.CreateViewController<CustomLeftViewController>();
                _communityLeaderboard.VotePressed += (item, category) =>
                {
                    SetLeftScreenViewController(null);
                    SetRightScreenViewController(null);

                    var message = $"Are you sure you want to vote for this {}\n" +
                            "(check #rules-and-info for the difficulty you need to play on):\n";
                    Player.Instance.GetSongsToImproveBeforeRankUp().ForEach(x => message += $"{DiscordCommunityShared.OstHelper.GetOstSongNameFromLevelId(x)}\n");
                    _rankUpViewController.Init("Rank Up", message, "Ok");
                    PresentViewController(_rankUpViewController);
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
            Category rankToView = _communityLeaderboard.selectedCategory;
            if (rankToView <= Category.None) rankToView = Player.Instance.rank;
            _communityLeaderboard.SetSong(difficultyLevel, rankToView);
        }

        public virtual void HandleRankPromptViewControllerDidFinish(SimpleDialogPromptViewController viewController, bool ok)
        {
            if (ok)
            {
                DismissViewController(viewController, immediately: true);
                string signed = DiscordCommunityShared.RSA.SignVote(Plugin.PlayerId, Player.Instance.rank + 1, false);
                Client.RequestRank(Plugin.PlayerId, Player.Instance.rank + 1, false, signed);
            }
            DismissViewController(viewController);
        }

        private void ReloadServerData()
        {
            Client.GetEventData(_levelCollections, songListViewController, Plugin.PlayerId.ToString());
        }
    }
}