using CustomUI.BeatSaber;
using TeamSaberPlugin.DiscordCommunityHelpers;
using TeamSaberPlugin.Misc;
using TeamSaberPlugin.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using static TeamSaberShared.SharedConstructs;
using Logger = TeamSaberShared.Logger;

/*
 * Created by Moon on 9/22/2018
 * A ViewController allowing me to show a leaderboard and other player data
 */

namespace TeamSaberPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeaderboardController : VRUIViewController
    {
        protected CustomLeaderboardTableView _leaderboard;

        public event Action<IDifficultyBeatmap> PlayPressed;
        public IDifficultyBeatmap selectedMap;
        public string selectedTeam = "-1";

        TextMeshProUGUI _songName;
        TextMeshProUGUI _team;
        Button _playButton;
        Button _pageLeftButton;
        Button _pageRightButton;

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

                _team = BeatSaberUI.CreateText(rectTransform, "Team", new Vector2());
                _team.fontSize = 4f;
                _team.alignment = TextAlignmentOptions.Center;
                _team.color = Color.gray;
                (_team.transform as RectTransform).anchorMin = new Vector2(.5f, 1f);
                (_team.transform as RectTransform).anchorMax = new Vector2(.5f, 1f);
                (_team.transform as RectTransform).anchoredPosition = new Vector2(0f, -15f);

                _pageLeftButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageLeftButton.transform as RectTransform).anchorMin = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchorMax = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchoredPosition = new Vector2(27f, -10f);
                Quaternion currentRot = (_pageLeftButton.transform as RectTransform).rotation;
                (_pageLeftButton.transform as RectTransform).rotation = Quaternion.Euler(currentRot.eulerAngles.x, currentRot.eulerAngles.y, currentRot.eulerAngles.z + 90);
                _pageLeftButton.interactable = true;
                _pageLeftButton.onClick.AddListener(() =>
                {
                    SetSong(selectedMap, --selectedTeam);
                    if (selectedTeam <= (int)Team.Team1) _pageLeftButton.interactable = false;
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
                    SetSong(selectedMap, ++selectedTeam);
                    if (selectedTeam >= Team.All) _pageRightButton.interactable = false;
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
                _team.gameObject.SetActive(false);
                _playButton.gameObject.SetActive(false);
                _pageLeftButton.gameObject.SetActive(false);
                _pageRightButton.gameObject.SetActive(false);

                selectedTeam = "-1";
            }
        }

        public void SetSong(IDifficultyBeatmap map, string teamId)
        {
            //Set globals
            selectedMap = map;
            selectedTeam = teamId;

            //Enable relevant views
            _leaderboard.gameObject.SetActive(true);
            _songName.gameObject.SetActive(true);
            _team.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(true);
            _pageLeftButton.gameObject.SetActive(true);
            _pageRightButton.gameObject.SetActive(true);

            //Set song name text and team text (and color)
            _songName.SetText(map.level.songName);
            _team.SetText(teamId.ToString());

            _team.color = Player.GetColorForTeam(teamId);
            if (teamId == Team.All)
            {
                _team.color = Color.green;
                _team.SetText("Mixed");
            }

            //Get leaderboard data
            Client.GetSongLeaderboard(this, SongIdHelper.GetSongIdFromLevelId(map.level.levelID), Rarity.All, teamId, teamId == Team.All);
        }

        public void Refresh()
        {
            if (selectedMap != null && selectedTeam != Team.None) SetSong(selectedMap, selectedTeam);
        }

        public void SetScores(List<CustomLeaderboardTableView.CustomScoreData> scores, int myScorePos, bool useTeamColors = false)
        {
            int num = (scores != null) ? scores.Count : 0;
            for (int j = num; j < 10; j++)
            {
                scores.Add(new CustomLeaderboardTableView.CustomScoreData(-1, string.Empty, j + 1, false));
            }

            _leaderboard.SetScores(scores, myScorePos, useTeamColors);
        }
    }
}
