﻿using DiscordCommunityPlugin.DiscordCommunityHelpers;
using DiscordCommunityPlugin.UI.FlowCoordinators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using static DiscordCommunityShared.SharedConstructs;
using Logger = DiscordCommunityShared.Logger;

/*
 * Created by Moon on 9/22/2018
 * A ViewController allowing me to show a leaderboard and other player data
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeaderboardController : VRUIViewController
    {
        protected CustomLeaderboardTableView _leaderboard;

        public event Action<GameplayOptions> PlayPressed;
        public event Action RequestRankPressed;
        public IStandardLevelDifficultyBeatmap selectedMap;
        public Rank selectedRank = Rank.None;

        TextMeshProUGUI _songName;
        TextMeshProUGUI _rank;
        TextMeshProUGUI _projectedTokens;
        Button _playButton;
        Button _pageLeftButton;
        Button _pageRightButton;
        Button _rankUpButton;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _leaderboard = gameObject.AddComponent<CustomLeaderboardTableView>();
                _leaderboard.transform.SetParent(rectTransform, false);
                _leaderboard.name = "Community Leaderboard";

                _songName = BaseUI.CreateText(rectTransform, "Song", new Vector2(0f, -10f));
                _songName.fontSize = 8f;
                _songName.alignment = TextAlignmentOptions.Center;
                _songName.gameObject.SetActive(false);

                _rank = BaseUI.CreateText(rectTransform, "Rank", new Vector2(0f, -15f));
                _rank.fontSize = 4f;
                _rank.alignment = TextAlignmentOptions.Center;
                _rank.color = Color.gray;
                _rank.gameObject.SetActive(false);

                _projectedTokens = BaseUI.CreateText(rectTransform, "Tokens", new Vector2());
                (_projectedTokens.transform as RectTransform).anchorMin = new Vector2(0f, 1f);
                (_projectedTokens.transform as RectTransform).anchorMax = new Vector2(0f, 1f);
                (_projectedTokens.transform as RectTransform).anchoredPosition = new Vector2(15f, -19f); //new Vector2(21f, -15f);
                _projectedTokens.fontSize = 6f;
                _projectedTokens.alignment = TextAlignmentOptions.Center;
                _projectedTokens.gameObject.SetActive(false);

                _rankUpButton = BaseUI.CreateUIButton(rectTransform, "QuitButton");
                BaseUI.SetButtonText(_rankUpButton, "Rank Up");
                (_rankUpButton.transform as RectTransform).anchorMin = new Vector2(1f, 1f);
                (_rankUpButton.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_rankUpButton.transform as RectTransform).anchoredPosition = new Vector2(-21f, -28f);
                (_rankUpButton.transform as RectTransform).sizeDelta = new Vector2(18f, 10f);
                _rankUpButton.gameObject.SetActive(false);
                _rankUpButton.onClick.AddListener(() => {
                    var rankUpModal = BaseUI.CreateViewController<ModalViewController>();
                    if (Player.Instance.CanRankUp())
                    {
                        if (Player.Instance.rank < Rank.Gold)
                        {
                            rankUpModal.Message =
                                $"You are about to rank up from {Player.Instance.rank} to {Player.Instance.rank + 1}.\n" +
                                "Are you sure you want to perform this action?";
                        }
                        else if (Player.Instance.rank == Rank.Gold)
                        {
                            rankUpModal.Message =
                            "You are about to spend 3 tokens and apply to rank up to Blue.\n" +
                            "Your Scoresaber profile will be submitted to the Blues, where it will then be voted on.\n" +
                            "Are you sure you want to perform this action?";
                        }
                        rankUpModal.Type = ModalViewController.ModalType.YesNo;
                        rankUpModal.YesCallback = () =>
                        {
                            string signed = DiscordCommunityShared.RSA.SignRankRequest(Plugin.PlayerId, Player.Instance.rank + 1, false);
                            Misc.Client.RequestRank(Plugin.PlayerId, Player.Instance.rank + 1, false, signed);

                            //Force the player back to the main menu so that their rank is refreshed for the next load
                            rankUpModal.DismissModalViewController(null, true);
                            rankUpModal.DontDismiss = true;
                            RequestRankPressed?.Invoke();
                        };
                    }
                    else if (Player.Instance.rank >= Rank.Gold && Player.Instance.tokens < 3)
                    {
                        rankUpModal.Message =
                            "You do not have enough tokens to rank up.\n" +
                            "3 tokens are required.";
                        rankUpModal.Type = ModalViewController.ModalType.Ok;
                    }
                    else
                    {
                        rankUpModal.Message = "You must improve your scores on the following songs\n" +
                        "(check #rules-and-info for the difficulty you need to play on):\n";
                        Player.Instance.GetSongsToImproveBeforeRankUp().ForEach(x => rankUpModal.Message += $"{DiscordCommunityShared.OstHelper.GetOstSongNameFromLevelId(x)}\n");
                    }

                    PresentModalViewController(rankUpModal, null);
                });

                _pageLeftButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageLeftButton.transform as RectTransform).anchorMin = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchorMax = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchoredPosition = new Vector2(27f, -10f);
                Quaternion currentRot = (_pageLeftButton.transform as RectTransform).rotation;
                (_pageLeftButton.transform as RectTransform).rotation = Quaternion.Euler(currentRot.eulerAngles.x, currentRot.eulerAngles.y, currentRot.eulerAngles.z + 90);
                _pageLeftButton.interactable = true;
                _pageLeftButton.gameObject.SetActive(false);
                _pageLeftButton.onClick.AddListener(() =>
                {
                    SetSong(selectedMap, --selectedRank);
                    if (selectedRank <= (int)Rank.White) _pageLeftButton.interactable = false;
                    _pageRightButton.interactable = true;
                });

                _pageRightButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageRightButton.transform as RectTransform).anchorMin = new Vector2(1f, 0.5f);
                (_pageRightButton.transform as RectTransform).anchorMax = new Vector2(1f, 0.5f);
                (_pageRightButton.transform as RectTransform).anchoredPosition = new Vector2(-27f, -10f);
                currentRot = (_pageRightButton.transform as RectTransform).rotation;
                (_pageRightButton.transform as RectTransform).rotation = Quaternion.Euler(currentRot.eulerAngles.x, currentRot.eulerAngles.y, currentRot.eulerAngles.z + 90);
                _pageRightButton.interactable = true;
                _pageRightButton.gameObject.SetActive(false);
                _pageRightButton.onClick.AddListener(() =>
                {
                    SetSong(selectedMap, ++selectedRank);
                    if (selectedRank >= Rank.All) _pageRightButton.interactable = false; //Master + 1 because [Master + 1] is the index of the mixed leaderboard
                    _pageLeftButton.interactable = true;
                });

                _playButton = BaseUI.CreateUIButton(rectTransform, "QuitButton");
                BaseUI.SetButtonText(_playButton, "Play");
                (_playButton.transform as RectTransform).anchorMin = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchoredPosition = new Vector2(-21f, -15f);
                (_playButton.transform as RectTransform).sizeDelta = new Vector2(18f, 10f);
                _playButton.gameObject.SetActive(false);
                _playButton.onClick.AddListener(() =>
                {
                    PlayPressed?.Invoke(new GameplayOptions());
                });
            }
            else if (!firstActivation && type == ActivationType.AddedToHierarchy)
            {
                //Disable relevant views
                _leaderboard.gameObject.SetActive(false);
                _songName.gameObject.SetActive(false);
                _rank.gameObject.SetActive(false);
                _playButton.gameObject.SetActive(false);
                _pageLeftButton.gameObject.SetActive(false);
                _pageRightButton.gameObject.SetActive(false);
                _projectedTokens.gameObject.SetActive(false);
                _rankUpButton.gameObject.SetActive(false);

                selectedRank = Rank.None;
            }
        }

        //When cloning existing objects, we can sometimes clone things we don't want
        //Here, we clean out cloned junk items from a leaderboard
        private static void CleanLeaderboards(Transform transform)
        {
            var container = transform.Find("Viewport").Find("Content");

            //An instance of the clones we want to destroy
            var cellClone = container.Find("LeaderboardTableCell(Clone)");

            //Loop through all the clones and destroy all their children
            while (cellClone != null)
            {
                DestroyImmediate(cellClone.gameObject);
                cellClone = container.Find("LeaderboardTableCell(Clone)");
            }
        }

        public void SetSong(IStandardLevelDifficultyBeatmap map, Rank rank)
        {
            //Set globals
            selectedMap = map;
            selectedRank = rank;

            //Enable relevant views
            _leaderboard.gameObject.SetActive(true);
            _songName.gameObject.SetActive(true);
            _rank.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(true);
            _pageLeftButton.gameObject.SetActive(true);
            _pageRightButton.gameObject.SetActive(true);
            _projectedTokens.gameObject.SetActive(Player.Instance.rank >= Rank.Gold && Player.Instance.rank <= Rank.Blue);
            _rankUpButton.gameObject.SetActive(Player.Instance.rank < Rank.Blue);

            //Set song name text and rank text (and color)
            _songName.SetText(map.level.songName);
            _rank.SetText(rank.ToString());
            _projectedTokens.SetText($"<size=60%>Tokens</size>\n{Player.Instance.tokens}\n<size=60%>Projected Tokens</size>\n{Player.Instance.projectedTokens}");

            if (rank >= Rank.White && rank <= Rank.Master) _rank.color = Player.GetColorForRank(rank);
            else if (rank == Rank.All)
            {
                _rank.color = Color.green;
                _rank.SetText("Mixed");
            }

            //Get leaderboard data
            Misc.Client.GetSongLeaderboard(this, Misc.SongIdHelper.GetSongIdFromLevelId(map.level.levelID), rank, rank == Rank.All);
        }

        public void SetScores(List<CustomLeaderboardTableView.CustomScoreData> scores, int myScorePos, bool useRankColors = false)
        {
            int num = (scores != null) ? scores.Count : 0;
            for (int j = num; j < 10; j++)
            {
                scores.Add(new CustomLeaderboardTableView.CustomScoreData(-1, string.Empty, j + 1, false));
            }

            _leaderboard.SetScores(scores, myScorePos, useRankColors);
        }
    }
}
