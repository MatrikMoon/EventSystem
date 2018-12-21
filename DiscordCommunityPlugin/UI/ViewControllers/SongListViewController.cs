using ChristmasVotePlugin.Misc;
using CustomUI.BeatSaber;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using static ChristmasShared.SharedConstructs;
using Logger = ChristmasShared.Logger;

/**
 * Created by andruzzzhka, from the BeatSaverMultiplayer plugin,
 * modified for the DiscordCommunityPlugin
 */

namespace ChristmasVotePlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class ItemListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<TableItem> ItemSelected;
        public event Action ReloadPressed;
        public event Action EventInfoDownloaded;
        public bool errorHappened = false;
        public Category SelectedCategory { get; private set; } = Category.Map;
        public List<TableItem> VotedOn { get; set; } = new List<TableItem>();

        private Button _pageUpButton;
        private Button _pageDownButton;
        private Button _downloadErrorReloadButton;
        private Button _mapButton;
        private Button _saberButton;
        private Button _avatarButton;
        private Button _platformButton;
        private Action<Category> _categoryChanged;

        public TableView itemsTableView;
        LevelListTableCell _itemTableCellInstance;
        TextMeshProUGUI _itemsDownloadingText;
        TextMeshProUGUI _downloadErrorText;

        List<TableItem> items = new List<TableItem>();
        List<TableItem> activeItems = new List<TableItem>();

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _itemTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                _categoryChanged += (category) => {
                    activeItems.Clear();
                    activeItems = items.Where(x => x.Category == category).ToList();
                    itemsTableView.ReloadData();
                };

                _mapButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", buttonText: "Maps");
                (_mapButton.transform as RectTransform).anchorMin = new Vector2(.2f, .9f);
                (_mapButton.transform as RectTransform).anchorMax = new Vector2(.2f, .9f);
                (_mapButton.transform as RectTransform).anchoredPosition = new Vector2(0, 0);
                (_mapButton.transform as RectTransform).sizeDelta = new Vector2(32, 10);
                _mapButton.onClick.AddListener(() =>
                {
                    SelectedCategory = Category.Map;
                    _categoryChanged?.Invoke(SelectedCategory);
                });

                _saberButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", buttonText: "Sabers");
                (_saberButton.transform as RectTransform).anchorMin = new Vector2(.4f, .9f);
                (_saberButton.transform as RectTransform).anchorMax = new Vector2(.4f, .9f);
                (_saberButton.transform as RectTransform).anchoredPosition = new Vector2(0, 0);
                (_saberButton.transform as RectTransform).sizeDelta = new Vector2(32, 10);
                _saberButton.onClick.AddListener(() =>
                {
                    SelectedCategory = Category.Saber;
                    _categoryChanged?.Invoke(SelectedCategory);
                });

                _avatarButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", buttonText: "Avatars");
                (_avatarButton.transform as RectTransform).anchorMin = new Vector2(.6f, .9f);
                (_avatarButton.transform as RectTransform).anchorMax = new Vector2(.6f, .9f);
                (_avatarButton.transform as RectTransform).anchoredPosition = new Vector2(0, 0);
                (_avatarButton.transform as RectTransform).sizeDelta = new Vector2(32, 10);
                _avatarButton.onClick.AddListener(() =>
                {
                    SelectedCategory = Category.Avatar;
                    _categoryChanged?.Invoke(SelectedCategory);
                });

                _platformButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", buttonText: "Platforms");
                (_platformButton.transform as RectTransform).anchorMin = new Vector2(.8f, .9f);
                (_platformButton.transform as RectTransform).anchorMax = new Vector2(.8f, .9f);
                (_platformButton.transform as RectTransform).anchoredPosition = new Vector2(1, 0);
                (_platformButton.transform as RectTransform).sizeDelta = new Vector2(34, 10);
                _platformButton.onClick.AddListener(() =>
                {
                    SelectedCategory = Category.Platform;
                    _categoryChanged?.Invoke(SelectedCategory);
                });

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -21f);
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    itemsTableView.PageScrollUp();
                });
                _pageUpButton.interactable = false;

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 10f);
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    itemsTableView.PageScrollDown();
                });
                _pageDownButton.interactable = false;

                itemsTableView = new GameObject().AddComponent<TableView>();
                itemsTableView.transform.SetParent(rectTransform, false);
                itemsTableView.SetField("_isInitialized", false);
                itemsTableView.SetField("_preallocatedCells", new TableView.CellsGroup[0]);
                itemsTableView.Init();

                RectMask2D viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<RectMask2D>().First(), itemsTableView.transform, false);
                viewportMask.transform.DetachChildren();
                itemsTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                (itemsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (itemsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (itemsTableView.transform as RectTransform).anchoredPosition = new Vector2(0f, -5.5f);
                (itemsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 50f);

                _itemsDownloadingText = BeatSaberUI.CreateText(rectTransform, "Downloading event info...", new Vector2(0f, -25f));
                _itemsDownloadingText.fontSize = 8f;
                _itemsDownloadingText.alignment = TextAlignmentOptions.Center;
                _itemsDownloadingText.rectTransform.anchorMin = new Vector2(.5f, .7f);
                _itemsDownloadingText.rectTransform.anchorMax = new Vector2(.5f, .7f);
                _itemsDownloadingText.rectTransform.sizeDelta = new Vector2(120f, 6f);

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
                    itemsTableView.gameObject.SetActive(false);
                    _pageUpButton.gameObject.SetActive(false);
                    _pageDownButton.gameObject.SetActive(false);
                    _downloadErrorText.gameObject.SetActive(false);
                    _itemsDownloadingText.gameObject.SetActive(true);
                    _downloadErrorReloadButton.gameObject.SetActive(false);
                    _mapButton.gameObject.SetActive(false);
                    _saberButton.gameObject.SetActive(false);
                    _avatarButton.gameObject.SetActive(false);
                    _platformButton.gameObject.SetActive(false);

                    _downloadErrorText.SetText("Generic Error");
                    errorHappened = false;

                    ReloadPressed?.Invoke();
                });

                itemsTableView.SetField("_pageUpButton", _pageUpButton);
                itemsTableView.SetField("_pageDownButton", _pageDownButton);

                itemsTableView.didSelectRowEvent += SongsTableView_DidSelectRow;
                itemsTableView.dataSource = this;

                //Set to view "Downloading event info..." until the songs are set
                itemsTableView.gameObject.SetActive(false);
                _pageUpButton.gameObject.SetActive(false);
                _pageDownButton.gameObject.SetActive(false);
                _downloadErrorText.gameObject.SetActive(false);
                _itemsDownloadingText.gameObject.SetActive(true);
                _downloadErrorReloadButton.gameObject.SetActive(false);
                _mapButton.gameObject.SetActive(false);
                _saberButton.gameObject.SetActive(false);
                _avatarButton.gameObject.SetActive(false);
                _platformButton.gameObject.SetActive(false);
            }
            else
            {
                itemsTableView.ReloadData();
            }
        }

        public void DownloadErrorHappened(string error)
        {
            itemsTableView.gameObject.SetActive(false);
            _pageUpButton.gameObject.SetActive(false);
            _pageDownButton.gameObject.SetActive(false);
            _downloadErrorText.gameObject.SetActive(true);
            _downloadErrorReloadButton.gameObject.SetActive(true);
            _itemsDownloadingText.gameObject.SetActive(false);
            _mapButton.gameObject.SetActive(false);
            _saberButton.gameObject.SetActive(false);
            _avatarButton.gameObject.SetActive(false);
            _platformButton.gameObject.SetActive(false);

            _downloadErrorText.SetText(error);
            errorHappened = true;
        }

        private void SongsTableView_DidSelectRow(TableView sender, int row)
        {
            ItemSelected?.Invoke(activeItems[row]);
        }

        public void SetItems(List<TableItem> items)
        {
            //Now that songs are being set, hide the "downloading" text
            if (_downloadErrorText.gameObject.activeSelf) return; //If there was an error earlier, don't continue
            itemsTableView.gameObject.SetActive(true);
            _pageUpButton.gameObject.SetActive(true);
            _pageDownButton.gameObject.SetActive(true);
            _itemsDownloadingText.gameObject.SetActive(false);
            _mapButton.gameObject.SetActive(true);
            _saberButton.gameObject.SetActive(true);
            _avatarButton.gameObject.SetActive(true);
            _platformButton.gameObject.SetActive(true);

            this.items = items;
            _categoryChanged?.Invoke(SelectedCategory);

            if (itemsTableView.dataSource != (TableView.IDataSource)this)
            {
                itemsTableView.dataSource = this;
            }
            else
            {
                itemsTableView.ReloadData();
            }

            itemsTableView.ScrollToRow(0, false);
            EventInfoDownloaded?.Invoke();
        }

        public void Refresh()
        {
            itemsTableView.ReloadData();
        }

        public TableCell CellForRow(int row)
        {
            LevelListTableCell cell = Instantiate(_itemTableCellInstance);

            TableItem item = activeItems[row];

            if (item.Category == Category.Map)
            {
                try
                {
                    cell.coverImage = SongIdHelper.GetLevelFromSongId(item.ItemId).coverImage;
                }
                catch
                {
                }
            }
            if (cell.coverImage == null) cell.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(x => x.name == "CoverImage").enabled = false;

            if (VotedOn.Contains(item)) cell.songName = $"<#666666>{item.Name}\n<size=80%>{item.SubName}</size>";
            else cell.songName = $"{item.Name}\n<size=80%>{item.SubName}</size>";
            cell.author = item.Author;
            cell.reuseIdentifier = "SongCell";

            return cell;
        }

        public int NumberOfRows()
        {
            return activeItems.Count;
        }

        public float RowHeight()
        {
            return 10f;
        }
    }
}
