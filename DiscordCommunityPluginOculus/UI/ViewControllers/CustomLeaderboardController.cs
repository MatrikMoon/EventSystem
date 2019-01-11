using CustomUI.BeatSaber;
using DiscordCommunityPlugin.DiscordCommunityHelpers;
using DiscordCommunityPlugin.Misc;
using DiscordCommunityPlugin.UI.Views;
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

        public event Action<IDifficultyBeatmap> PlayPressed;
        public event Action RequestRankPressed;
        public IDifficultyBeatmap selectedMap;
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

                _songName = BeatSaberUI.CreateText(rectTransform, "Song", new Vector2());
                _songName.fontSize = 8f;
                _songName.alignment = TextAlignmentOptions.Center;
                (_songName.transform as RectTransform).anchorMin = new Vector2(.5f, 1f);
                (_songName.transform as RectTransform).anchorMax = new Vector2(.5f, 1f);
                (_songName.transform as RectTransform).anchoredPosition = new Vector2(0f, -10f);

                _rank = BeatSaberUI.CreateText(rectTransform, "Rank", new Vector2());
                _rank.fontSize = 4f;
                _rank.alignment = TextAlignmentOptions.Center;
                _rank.color = Color.gray;
                (_rank.transform as RectTransform).anchorMin = new Vector2(.5f, 1f);
                (_rank.transform as RectTransform).anchorMax = new Vector2(.5f, 1f);
                (_rank.transform as RectTransform).anchoredPosition = new Vector2(0f, -15f);

                _projectedTokens = BeatSaberUI.CreateText(rectTransform, "Tokens", new Vector2());
                (_projectedTokens.transform as RectTransform).anchorMin = new Vector2(0f, 1f);
                (_projectedTokens.transform as RectTransform).anchorMax = new Vector2(0f, 1f);
                (_projectedTokens.transform as RectTransform).anchoredPosition = new Vector2(15f, -19f); //new Vector2(21f, -15f);
                _projectedTokens.fontSize = 6f;
                _projectedTokens.alignment = TextAlignmentOptions.Center;

                _rankUpButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _rankUpButton.SetButtonText("Rank Up");
                (_rankUpButton.transform as RectTransform).anchorMin = new Vector2(1f, 1f);
                (_rankUpButton.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_rankUpButton.transform as RectTransform).anchoredPosition = new Vector2(-17f, -21f);
                (_rankUpButton.transform as RectTransform).sizeDelta = new Vector2(28f, 10f);
                _rankUpButton.onClick.AddListener(() => RequestRankPressed?.Invoke());

                _pageLeftButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageLeftButton.transform as RectTransform).anchorMin = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchorMax = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchoredPosition = new Vector2(27f, -10f);
                Quaternion currentRot = (_pageLeftButton.transform as RectTransform).rotation;
                (_pageLeftButton.transform as RectTransform).rotation = Quaternion.Euler(currentRot.eulerAngles.x, currentRot.eulerAngles.y, currentRot.eulerAngles.z + 90);
                _pageLeftButton.interactable = true;
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
                _pageRightButton.onClick.AddListener(() =>
                {
                    SetSong(selectedMap, ++selectedRank);
                    if (selectedRank >= Rank.All) _pageRightButton.interactable = false;
                    _pageLeftButton.interactable = true;
                });

                _playButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _playButton.SetButtonText("Play");
                (_playButton.transform as RectTransform).anchorMin = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchoredPosition = new Vector2(-17f, -8f);
                (_playButton.transform as RectTransform).sizeDelta = new Vector2(28f, 10f);
                _playButton.onClick.AddListener(() =>
                {
                    PlayPressed?.Invoke(selectedMap);
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

        public void SetSong(IDifficultyBeatmap map, Rank rank)
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
            _projectedTokens.gameObject.SetActive(Player.Instance.rarity >= Rank.Gold && Player.Instance.rarity <= Rank.Blue);
            _rankUpButton.gameObject.SetActive(Player.Instance.rarity < Rank.Blue);

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
            Client.GetSongLeaderboard(this, SongIdHelper.GetSongIdFromLevelId(map.level.levelID), rank, rank == Rank.All);
        }

        public void Refresh()
        {
            if (selectedMap != null && selectedRank != Rank.None) SetSong(selectedMap, selectedRank);
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
