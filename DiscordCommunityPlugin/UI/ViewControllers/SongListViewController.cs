using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using DiscordCommunityPlugin.DiscordCommunityHelpers;
using Logger = DiscordCommunityShared.Logger;
using DiscordCommunityPlugin.Misc;

/**
 * Created by andruzzzhka, from the BeatSaverMultiplayer plugin,
 * modified for the DiscordCommunityPlugin
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class SongListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<IStandardLevel, GameplayOptions> SongPlayPressed;
        public event Action SongsDownloaded;
        public bool errorHappened = false;

        private Button _pageUpButton;
        private Button _pageDownButton;
        private int _currentRow;
        private string selectWhenLoaded;

        public TableView songsTableView;
        StandardLevelListTableCell _songTableCellInstance;
        TextMeshProUGUI _songsDownloadingText;
        TextMeshProUGUI _downloadErrorText;

        List<IStandardLevel> availableSongs = new List<IStandardLevel>();

        protected PlatformLeaderboardViewController _globalLeaderboard;

        protected CustomLeaderboardController _communityLeaderboard;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _songTableCellInstance = Resources.FindObjectsOfTypeAll<StandardLevelListTableCell>().First(x => (x.name == "StandardLevelListTableCell"));

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14f);
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    songsTableView.PageScrollUp();
                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    songsTableView.PageScrollDown();
                });
                _pageDownButton.interactable = false;

                songsTableView = new GameObject().AddComponent<TableView>();
                songsTableView.transform.SetParent(rectTransform, false);

                Mask viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<Mask>().First(), songsTableView.transform, false);
                viewportMask.transform.DetachChildren();
                songsTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                (songsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (songsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (songsTableView.transform as RectTransform).position = new Vector3(0f, 0f, 2.4f);
                (songsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                _songsDownloadingText = BaseUI.CreateText(rectTransform, "Downloading weekly songs...", new Vector2(0f, -25f));
                _songsDownloadingText.fontSize = 8f;
                _songsDownloadingText.alignment = TextAlignmentOptions.Center;
                _songsDownloadingText.rectTransform.sizeDelta = new Vector2(120f, 6f);

                _downloadErrorText = BaseUI.CreateText(rectTransform, "Generic Error", new Vector2(0f, -25f));
                _downloadErrorText.fontSize = 8f;
                _downloadErrorText.alignment = TextAlignmentOptions.Center;
                _downloadErrorText.rectTransform.sizeDelta = new Vector2(120f, 6f);

                songsTableView.SetField("_pageUpButton", _pageUpButton);
                songsTableView.SetField("_pageDownButton", _pageDownButton);

                songsTableView.didSelectRowEvent += SongsTableView_DidSelectRow;
                songsTableView.dataSource = this;

                //Set to view "Downloading weekly songs..." until the songs are set
                songsTableView.gameObject.SetActive(false);
                _pageUpButton.gameObject.SetActive(false);
                _pageDownButton.gameObject.SetActive(false);
                _downloadErrorText.gameObject.SetActive(false);
                _songsDownloadingText.gameObject.SetActive(true);

                //Currently selected row
                _currentRow = -1;
            }
            else
            {
                songsTableView.ReloadData();
            }
        }

        protected override void LeftAndRightScreenViewControllers(out VRUIViewController leftScreenViewController, out VRUIViewController rightScreenViewController)
        {
            if (_communityLeaderboard == null)
            {
                _communityLeaderboard = BaseUI.CreateViewController<CustomLeaderboardController>();
            }
            if (_globalLeaderboard == null)
            {
                _globalLeaderboard = Instantiate(Resources.FindObjectsOfTypeAll<PlatformLeaderboardViewController>().First());
                _globalLeaderboard.name = "Community Global Leaderboard";
                CleanPlatformLeaderboard(_globalLeaderboard);
            }
            
            leftScreenViewController = _communityLeaderboard;
            rightScreenViewController = _globalLeaderboard;
        }

        //Cleans the extra cloned junk out of a leaderboard
        private static void CleanPlatformLeaderboard(PlatformLeaderboardViewController plvc)
        {
            //Clean out leaderboard table view
            LeaderboardTableView ltv = plvc.GetField<LeaderboardTableView>("_leaderboardTableView");
            if (ltv == null) return;

            Transform transform = ltv.transform;
            var container = transform.Find("Viewport").Find("Content");

            //An instance of the clones we want to destroy
            var cellClone = container.Find("LeaderboardTableCell(Clone)");

            //Loop through all the clones and destroy all their children
            while (cellClone != null)
            {
                DestroyImmediate(cellClone.gameObject);
                cellClone = container.Find("LeaderboardTableCell(Clone)");
            }

            //Clean out scope selection view
            SimpleSegmentedControl ssc = plvc.GetField<SimpleSegmentedControl>("_scopeSegmentedControl");
            if (ssc == null) return;

            //Transform of the scope selection
            transform = ssc.transform;

            //Clones we want to destroy
            var first = transform.Find("FirstTextSegmentedControlCell(Clone)");
            var middle = transform.Find("MiddleTextSegmentedControlCell(Clone)");
            var last = transform.Find("LastTextSegmentedControlCell(Clone)");

            //Destroy them
            if (first != null) DestroyImmediate(first.gameObject);
            if (middle != null) DestroyImmediate(middle.gameObject);
            if (last != null) DestroyImmediate(last.gameObject);
        }

        public void DownloadErrorHappened(string error)
        {
            songsTableView.gameObject.SetActive(false);
            _pageUpButton.gameObject.SetActive(false);
            _pageDownButton.gameObject.SetActive(false);
            _downloadErrorText.gameObject.SetActive(true);
            _songsDownloadingText.gameObject.SetActive(false);

            _downloadErrorText.SetText(error);
            errorHappened = true;
        }

        private void SongsTableView_DidSelectRow(TableView sender, int row)
        {
            //Change global leaderboard view
            IStandardLevelDifficultyBeatmap difficultyLevel = Player.Instance.GetMapForRank(availableSongs[row]);
            GameplayMode gameMode = Player.Instance.desiredModes[SongIdHelper.GetSongIdFromLevelId(availableSongs[row].levelID)];
            _globalLeaderboard.Init(difficultyLevel, gameMode);
            _globalLeaderboard.Refresh();

            //Change community leaderboard view
            //Use the currently selected rank, if it exists
            int rankToView = _communityLeaderboard.selectedRank;
            if (rankToView <= -1) rankToView = (int)Player.Instance.rank;
            _communityLeaderboard.SetSong(difficultyLevel, rankToView);

            //Do song selected action
            _communityLeaderboard.PlayPressed -= PlayPressed_Listener;
            _communityLeaderboard.PlayPressed += PlayPressed_Listener;


            //Set current row
            _currentRow = row;
        }

        private void PlayPressed_Listener(GameplayOptions options)
        {
            SongPlayPressed?.Invoke(availableSongs[_currentRow], options);
        }

        public void SelectWhenLoaded(string levelId)
        {
            selectWhenLoaded = levelId;
        }

        public void SetSongs(List<IStandardLevel> levels)
        {
            try
            {
                Logger.Info($"SHOWING {levels.Count} songs");
                levels.ForEach(x => Logger.Info($"SHOWING: {x.songName}"));
                Logger.Info("CONTINUING TO SHOW");
            }
            catch (Exception e)
            {
                Logger.Error($"ERROR ADDING SONGS: {e}");
                DownloadErrorHappened($"ERROR ADDING SONGS: {e}");
            }

            //Now that songs are being set, hide the "downloading" text
            if (_downloadErrorText.gameObject.activeSelf) return; //If there was an error earlier, don't continue
            songsTableView.gameObject.SetActive(true);
            _pageUpButton.gameObject.SetActive(true);
            _pageDownButton.gameObject.SetActive(true);
            _songsDownloadingText.gameObject.SetActive(false);

            availableSongs = levels;

            if (songsTableView.dataSource != (TableView.IDataSource)this)
            {
                songsTableView.dataSource = this;
            }
            else
            {
                songsTableView.ReloadData();
            }

            songsTableView.ScrollToRow(0, false);
            if (selectWhenLoaded != null)
            {
                int songIndex = availableSongs.IndexOf(availableSongs.Where(x => x.levelID == selectWhenLoaded).First());
                songsTableView.SelectRow(songIndex, true);
            }
            SongsDownloaded?.Invoke();
        }

        public bool HasSongs()
        {
            return availableSongs.Count > 0;
        }

        public TableCell CellForRow(int row)
        {
            StandardLevelListTableCell cell = Instantiate(_songTableCellInstance);

            IStandardLevel song = availableSongs[row];

            cell.coverImage = song.coverImage;
            cell.songName = $"{song.songName}\n<size=80%>{song.songSubName}</size>";
            cell.author = song.songAuthorName;

            cell.reuseIdentifier = "SongCell";

            return cell;
        }

        public int NumberOfRows()
        {
            return availableSongs.Count;
        }

        public float RowHeight()
        {
            return 10f;
        }
    }
}
