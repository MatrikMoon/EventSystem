﻿using DiscordCommunityPlugin.UI.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DiscordCommunityPlugin.UI.FlowCoordinators
{
    class MainModFlowCoordinator : FlowCoordinator
    {
        public MainMenuViewController mmvc;

        private SongListViewController _songListViewController;

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

        public void OpenSongsList()
        {
            if (mmvc == null) return;
            if (_songListViewController == null)
            {
                _songListViewController = BaseUI.CreateViewController<SongListViewController>();
            }
            mmvc.PresentModalViewController(_songListViewController, null, false);
            List<IStandardLevel> availableSongs = new List<IStandardLevel>();
            _levelCollections.GetLevels(GameplayMode.SoloStandard).AsParallel().ForAll(x => availableSongs.Add(x));

            _songListViewController.SetSongs(availableSongs);
            _songListViewController.UpdateViewController(true);
        }
    }
}