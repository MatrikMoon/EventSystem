using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

/**
 * Created by andruzzzhka, from the BeatSaverMultiplayer plugin
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class SongListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<IStandardLevel> SongSelected;
        public bool errorHappened = false;

        private Button _pageUpButton;
        private Button _pageDownButton;

        TableView _songsTableView;
        StandardLevelListTableCell _songTableCellInstance;
        TextMeshProUGUI _songsDownloadingText;
        TextMeshProUGUI _downloadErrorText;

        List<IStandardLevel> availableSongs = new List<IStandardLevel>();

        protected HowToPlayViewController _howToPlayViewController;

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
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollUp();

                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollDown();

                });
                _pageDownButton.interactable = false;

                _songsTableView = new GameObject().AddComponent<TableView>();
                _songsTableView.transform.SetParent(rectTransform, false);

                Mask viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<Mask>().First(), _songsTableView.transform, false);
                viewportMask.transform.DetachChildren();
                _songsTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                (_songsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (_songsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (_songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_songsTableView.transform as RectTransform).position = new Vector3(0f, 0f, 2.4f);
                (_songsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                _songsDownloadingText = BaseUI.CreateText(rectTransform, "Downloading weekly songs...", new Vector2(0f, -25f));
                _songsDownloadingText.fontSize = 8f;
                _songsDownloadingText.alignment = TextAlignmentOptions.Center;
                _songsDownloadingText.rectTransform.sizeDelta = new Vector2(120f, 6f);

                _downloadErrorText = BaseUI.CreateText(rectTransform, "Generic Error", new Vector2(0f, -25f));
                _downloadErrorText.fontSize = 8f;
                _downloadErrorText.alignment = TextAlignmentOptions.Center;
                _downloadErrorText.rectTransform.sizeDelta = new Vector2(120f, 6f);

                _songsTableView.SetField("_pageUpButton", _pageUpButton);
                _songsTableView.SetField("_pageDownButton", _pageDownButton);

                _songsTableView.didSelectRowEvent += SongsTableView_DidSelectRow;
                _songsTableView.dataSource = this;

                //Set to view "Downloading weekly songs..." until the songs are set
                _songsTableView.gameObject.SetActive(false);
                _pageUpButton.gameObject.SetActive(false);
                _pageDownButton.gameObject.SetActive(false);
                _downloadErrorText.gameObject.SetActive(false);
                _songsDownloadingText.gameObject.SetActive(true);
            }
            else
            {
                _songsTableView.ReloadData();
            }
        }

        /*
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void LeftAndRightScreenViewControllers(out VRUIViewController leftScreenViewController, out VRUIViewController rightScreenViewController)
        {
            _howToPlayViewController.Init(showTutorialButton: true);
            leftScreenViewController = _howToPlayViewController;
            rightScreenViewController = null;
        }
        */

        public void DownloadErrorHappened(string error)
        {
            _songsTableView.gameObject.SetActive(false);
            _pageUpButton.gameObject.SetActive(false);
            _pageDownButton.gameObject.SetActive(false);
            _downloadErrorText.gameObject.SetActive(true);
            _songsDownloadingText.gameObject.SetActive(false);

            _downloadErrorText.SetText(error);
            errorHappened = true;
        }

        private void SongsTableView_DidSelectRow(TableView sender, int row)
        {
            SongSelected?.Invoke(availableSongs[row]);
        }

        public void SetSongs(List<IStandardLevel> levels)
        {
            try
            {
                DiscordCommunityShared.Logger.Info($"SHOWING {levels.Count} songs");
                levels.ForEach(x => DiscordCommunityShared.Logger.Info($"SHOWING: {x.songName}"));
                DiscordCommunityShared.Logger.Info("CONTINUING TO SHOW");
            }
            catch (Exception e)
            {
                DiscordCommunityShared.Logger.Error($"ERROR ADDING SONGS: {e}");
                DownloadErrorHappened($"ERROR ADDING SONGS: {e}");
            }

            //Now that songs are being set, hide the "downloading" text
            if (_downloadErrorText.gameObject.activeSelf) return; //If there was an error earlier, don't continue
            _songsTableView.gameObject.SetActive(true);
            _pageUpButton.gameObject.SetActive(true);
            _pageDownButton.gameObject.SetActive(true);
            _songsDownloadingText.gameObject.SetActive(false);

            availableSongs = levels;

            if (_songsTableView.dataSource != (TableView.IDataSource)this)
            {
                _songsTableView.dataSource = this;
            }
            else
            {
                _songsTableView.ReloadData();
            }

            _songsTableView.ScrollToRow(0, false);
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
