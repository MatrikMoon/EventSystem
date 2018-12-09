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
using static DiscordCommunityShared.SharedConstructs;
using CustomUI.BeatSaber;

/**
 * Created by andruzzzhka, from the BeatSaverMultiplayer plugin,
 * modified for the DiscordCommunityPlugin
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class SongListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<IBeatmapLevel> SongListRowSelected;
        public event Action ReloadPressed;
        public event Action SongsDownloaded;
        public bool errorHappened = false;

        private Button _pageUpButton;
        private Button _pageDownButton;
        private Button _downloadErrorReloadButton;
        private string selectWhenLoaded;

        public TableView songsTableView;
        LevelListTableCell _songTableCellInstance;
        TextMeshProUGUI _songsDownloadingText;
        TextMeshProUGUI _downloadErrorText;

        List<IBeatmapLevel> availableSongs = new List<IBeatmapLevel>();

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _songTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -10f);
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    songsTableView.PageScrollUp();
                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 10f);
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    songsTableView.PageScrollDown();
                });
                _pageDownButton.interactable = false;

                songsTableView = new GameObject().AddComponent<TableView>();
                songsTableView.transform.SetParent(rectTransform, false);
                songsTableView.SetField("_isInitialized", false);
                songsTableView.SetField("_preallocatedCells", new TableView.CellsGroup[0]);
                songsTableView.Init();

                RectMask2D viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<RectMask2D>().First(), songsTableView.transform, false);
                viewportMask.transform.DetachChildren();
                songsTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                (songsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (songsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);

                _songsDownloadingText = BeatSaberUI.CreateText(rectTransform, "Downloading weekly songs...", new Vector2(0f, -25f));
                _songsDownloadingText.fontSize = 8f;
                _songsDownloadingText.alignment = TextAlignmentOptions.Center;
                _songsDownloadingText.rectTransform.anchorMin = new Vector2(.5f, .7f);
                _songsDownloadingText.rectTransform.anchorMax = new Vector2(.5f, .7f);
                _songsDownloadingText.rectTransform.sizeDelta = new Vector2(120f, 6f);

                _downloadErrorText = BeatSaberUI.CreateText(rectTransform, "Generic Error", new Vector2(0f, 0f));
                _downloadErrorText.fontSize = 8f;
                _downloadErrorText.alignment = TextAlignmentOptions.Center;
                _downloadErrorText.rectTransform.anchorMin = new Vector2(.5f, .5f);
                _downloadErrorText.rectTransform.anchorMax = new Vector2(.5f, .5f);
                _downloadErrorText.rectTransform.sizeDelta = new Vector2(120f, 6f);

                _downloadErrorReloadButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _downloadErrorReloadButton.SetButtonText("Reload");
                (_downloadErrorReloadButton.transform as RectTransform).anchorMin = new Vector2(.5f, 0);
                (_downloadErrorReloadButton.transform as RectTransform).anchorMax = new Vector2(.5f, 0);
                (_downloadErrorReloadButton.transform as RectTransform).sizeDelta = new Vector2(38, 10);
                (_downloadErrorReloadButton.transform as RectTransform).anchoredPosition = new Vector2(0, 10f);
                _downloadErrorReloadButton.onClick.AddListener(() =>
                {
                    songsTableView.gameObject.SetActive(false);
                    _pageUpButton.gameObject.SetActive(false);
                    _pageDownButton.gameObject.SetActive(false);
                    _downloadErrorText.gameObject.SetActive(false);
                    _songsDownloadingText.gameObject.SetActive(true);
                    _downloadErrorReloadButton.gameObject.SetActive(false);

                    _downloadErrorText.SetText("Generic Error");
                    errorHappened = false;

                    ReloadPressed?.Invoke();
                });
                _downloadErrorReloadButton.gameObject.SetActive(false);

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
            }
            else
            {
                songsTableView.ReloadData();
            }
        }

        public void DownloadErrorHappened(string error)
        {
            songsTableView.gameObject.SetActive(false);
            _pageUpButton.gameObject.SetActive(false);
            _pageDownButton.gameObject.SetActive(false);
            _downloadErrorText.gameObject.SetActive(true);
            _downloadErrorReloadButton.gameObject.SetActive(true);
            _songsDownloadingText.gameObject.SetActive(false);

            _downloadErrorText.SetText(error);
            errorHappened = true;
        }

        private void SongsTableView_DidSelectRow(TableView sender, int row)
        {
            SongListRowSelected?.Invoke(availableSongs[row]);
        }

        public void SelectWhenLoaded(string levelId)
        {
            selectWhenLoaded = levelId;
        }

        public void SetSongs(List<IBeatmapLevel> levels)
        {
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
            LevelListTableCell cell = Instantiate(_songTableCellInstance);

            IBeatmapLevel song = availableSongs[row];

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
