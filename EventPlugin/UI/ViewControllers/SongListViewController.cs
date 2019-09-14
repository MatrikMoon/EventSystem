using CustomUI.BeatSaber;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventPlugin.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

/**
 * Created by andruzzzhka, from the BeatSaverMultiplayer plugin,
 * modified for the EventPlugin
 */

namespace EventPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class SongListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<Song> SongListRowSelected;
        public event Action ReloadPressed;
        public event Action SongsDownloaded;
        public bool errorHappened = false;

        private Button _pageUpButton;
        private Button _pageDownButton;
        private Button _downloadErrorReloadButton;
        private string selectWhenLoaded;

        TableView songsTableView;
        TableViewScroller _songTableViewScroller;
        LevelListTableCell _songTableCellInstance;
        TextMeshProUGUI _infoText;

        List<Song> availableSongs = new List<Song>();

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _songTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -3f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 6f);
                _pageUpButton.onClick.AddListener(() =>
                {
                    _songTableViewScroller.PageScrollUp();
                    songsTableView.RefreshScrollButtons();
                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 6f);
                _pageDownButton.onClick.AddListener(() =>
                {
                    _songTableViewScroller.PageScrollDown();
                    songsTableView.RefreshScrollButtons();
                });
                _pageDownButton.interactable = false;

                //Courtesy of andruzzzhka's scroll overlap fix
                RectTransform container = new GameObject("Content", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.anchorMin = new Vector2(0.3f, 0.5f);
                container.anchorMax = new Vector2(0.7f, 0.5f);
                container.sizeDelta = new Vector2(0f, 60f);

                var tableGO = new GameObject("CustomTableView");
                tableGO.SetActive(false);
                songsTableView = tableGO.AddComponent<TableView>();
                songsTableView.gameObject.AddComponent<RectMask2D>();
                songsTableView.transform.SetParent(container, false);

                songsTableView.SetField("_isInitialized", false);
                songsTableView.SetField("_preallocatedCells", new TableView.CellsGroup[0]);
                songsTableView.InvokeMethod("Init");

                (songsTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (songsTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
                (songsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, 0f);

                _infoText = BeatSaberUI.CreateText(rectTransform, "Downloading songs...", new Vector2(0f, -25f));
                _infoText.fontSize = 8f;
                _infoText.alignment = TextAlignmentOptions.Center;
                _infoText.rectTransform.anchorMin = new Vector2(.5f, .9f);
                _infoText.rectTransform.anchorMax = new Vector2(.5f, .9f);
                _infoText.rectTransform.sizeDelta = new Vector2(120f, 6f);
                _infoText.enableWordWrapping = true;

                _downloadErrorReloadButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", () =>
                {
                    songsTableView.gameObject.SetActive(false);
                    _pageUpButton.gameObject.SetActive(false);
                    _pageDownButton.gameObject.SetActive(false);
                    _infoText.gameObject.SetActive(true);
                    _downloadErrorReloadButton.gameObject.SetActive(false);

                    _infoText.SetText("Downloading songs...");
                    errorHappened = false;

                    ReloadPressed?.Invoke();
                }, "Reload");
                (_downloadErrorReloadButton.transform as RectTransform).anchorMin = new Vector2(.5f, 0);
                (_downloadErrorReloadButton.transform as RectTransform).anchorMax = new Vector2(.5f, 0);
                (_downloadErrorReloadButton.transform as RectTransform).sizeDelta = new Vector2(38, 10);
                (_downloadErrorReloadButton.transform as RectTransform).anchoredPosition = new Vector2(0, 10f);
                _downloadErrorReloadButton.gameObject.SetActive(false);

                songsTableView.SetField("_pageUpButton", _pageUpButton);
                songsTableView.SetField("_pageDownButton", _pageDownButton);

                //Following fix courtesy of superrob's multiplayer fork
                RectTransform viewport = new GameObject("Viewport").AddComponent<RectTransform>();
                viewport.SetParent(songsTableView.transform as RectTransform, false);
                viewport.sizeDelta = new Vector2(0f, 58f);
                songsTableView.Init();
                songsTableView.SetField("_scrollRectTransform", viewport);

                songsTableView.didSelectCellWithIdxEvent += SongsTableView_didSelectCellWithIdxEvent;
                songsTableView.dataSource = this;
                tableGO.SetActive(true);
                _songTableViewScroller = songsTableView.GetField<TableViewScroller>("_scroller");

                //Set to view "Downloading songs..." until the songs are set
                songsTableView.gameObject.SetActive(false);
                _pageUpButton.gameObject.SetActive(false);
                _pageDownButton.gameObject.SetActive(false);
                _infoText.gameObject.SetActive(true);
            }
            else
            {
                songsTableView.ReloadData();
            }
        }

        public void ErrorHappened(string error)
        {
            songsTableView.gameObject.SetActive(false);
            _pageUpButton.gameObject.SetActive(false);
            _pageDownButton.gameObject.SetActive(false);
            _downloadErrorReloadButton.gameObject.SetActive(true);
            _infoText.gameObject.SetActive(true);

            _infoText.SetText(error);
            errorHappened = true;
        }

        private void SongsTableView_didSelectCellWithIdxEvent(TableView sender, int row)
        {
            SongListRowSelected?.Invoke(availableSongs[row]);
        }

        public void SelectWhenLoaded(string levelId)
        {
            selectWhenLoaded = levelId;
        }

        public void SetSongs(List<Song> levels)
        {
            //Now that songs are being set, hide the "downloading" text
            if (errorHappened) return; //If there was an error earlier, don't continue
            songsTableView.gameObject.SetActive(true);
            _pageUpButton.gameObject.SetActive(true);
            _pageDownButton.gameObject.SetActive(true);
            _infoText.gameObject.SetActive(false);

            availableSongs = levels;

            if (songsTableView.dataSource != (TableView.IDataSource)this)
            {
                songsTableView.dataSource = this;
            }
            else
            {
                songsTableView.ReloadData();
            }

            songsTableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
            if (selectWhenLoaded != null)
            {
                int songIndex = availableSongs.IndexOf(availableSongs.Where(x => x.Beatmap?.level.levelID == selectWhenLoaded).First());
                songsTableView.SelectCellWithIdx(songIndex, true);
            }
            SongsDownloaded?.Invoke();
        }

        public bool HasSongs()
        {
            return availableSongs.Count > 0;
        }

        public TableCell CellForIdx(TableView tableView, int row)
        {
            LevelListTableCell cell = Instantiate(_songTableCellInstance);

            IPreviewBeatmapLevel song = availableSongs[row].PreviewBeatmap;

            cell.reuseIdentifier = "SongCell";
            cell.SetDataFromLevelAsync(song);
            cell.SetField("_bought", true);

            return cell;
        }

        public int NumberOfCells()
        {
            return availableSongs.Count;
        }

        public float CellSize()
        {
            return 10f;
        }
    }
}
