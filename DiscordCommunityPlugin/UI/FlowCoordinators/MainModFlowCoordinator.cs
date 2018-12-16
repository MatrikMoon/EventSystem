using ChristmasVotePlugin.Misc;
using ChristmasVotePlugin.UI.ViewControllers;
using CustomUI.BeatSaber;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRUI;
using static DiscordCommunityShared.SharedConstructs;

namespace ChristmasVotePlugin.UI.FlowCoordinators
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class MainModFlowCoordinator : FlowCoordinator
    {
        public MainFlowCoordinator mfc;
        public MainMenuViewController mmvc;
        public ItemListViewController itemListViewController;

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
                _rankUpViewController.didFinishEvent += HandleVotePromptViewControllerDidFinish;

                ProvideInitialViewControllers(_mainModNavigationController, _communityLeaderboard, _globalLeaderboard);
                OpenSongsList();
            }
        }

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            if (deactivationType == DeactivationType.RemovedFromHierarchy)
            {
                _rankUpViewController.didFinishEvent -= HandleVotePromptViewControllerDidFinish;
            }
        }

        public void PresentMainModUI()
        {
            mfc.InvokeMethod("PresentFlowCoordinatorOrAskForTutorial", this);
        }

        public void OpenSongsList(string songToSelectWhenLoaded = null)
        {
            if (itemListViewController == null)
            {
                itemListViewController = BeatSaberUI.CreateViewController<ItemListViewController>();
            }
            if (_levelCollections == null)
            {
                _levelCollections = Resources.FindObjectsOfTypeAll<LevelCollectionSO>().First();
            }
            if (_mainModNavigationController.GetField<List<VRUIViewController>>("_viewControllers").IndexOf(itemListViewController) < 0)
            {
                SetViewControllersToNavigationConctroller(_mainModNavigationController, new VRUIViewController[] { itemListViewController });

                itemListViewController.ItemSelected += ItemListItemSelected;
                itemListViewController.ReloadPressed += () =>
                {
                    mfc.InvokeMethod("DismissFlowCoordinator", this, null, false);
                };
                ReloadServerData();
            }
        }

        private void ItemListItemSelected(TableItem item)
        {
            //Open up the custom/global leaderboard pane when we need to
            if (_communityLeaderboard == null)
            {
                _communityLeaderboard = BeatSaberUI.CreateViewController<CustomLeftViewController>();
                _communityLeaderboard.VotePressed += (selectedItem) =>
                {
                    SetLeftScreenViewController(null);
                    SetRightScreenViewController(null);

                    var message = $"Are you sure you want to vote for: \"{_communityLeaderboard.SelectedItem.Name}\"\n";
                    _rankUpViewController.Init("Submit Vote", message, "Yes", "No");
                    PresentViewController(_rankUpViewController);
                };
            }
            SetLeftScreenViewController(_communityLeaderboard);

            /*
            if (_globalLeaderboard == null)
            {
                _globalLeaderboard = Resources.FindObjectsOfTypeAll<PlatformLeaderboardViewController>().First();
                _globalLeaderboard.name = "Community Global Leaderboard";
            }
            SetRightScreenViewController(_globalLeaderboard);
            */

            //Change the item viewed
            _communityLeaderboard.SetItem(item, itemListViewController);
        }

        public virtual void HandleVotePromptViewControllerDidFinish(SimpleDialogPromptViewController viewController, bool ok)
        {
            if (ok)
            {
                DismissViewController(viewController, immediately: true);
                string signed = DiscordCommunityShared.RSA.SignVote(Plugin.PlayerId, _communityLeaderboard.SelectedItem.ItemId, _communityLeaderboard.SelectedItem.Category);
                Client.SubmitVote(Plugin.PlayerId, _communityLeaderboard.SelectedItem.ItemId, _communityLeaderboard.SelectedItem.Category, signed, (b) =>
                {
                    itemListViewController.VotedOn.RemoveAll(x => x.Category == _communityLeaderboard.SelectedItem.Category);
                    itemListViewController.VotedOn.Add(_communityLeaderboard.SelectedItem);
                });
            }
            DismissViewController(viewController);
        }

        private void ReloadServerData()
        {
            Client.GetEventData(_levelCollections, itemListViewController, Plugin.PlayerId.ToString());
        }
    }
}