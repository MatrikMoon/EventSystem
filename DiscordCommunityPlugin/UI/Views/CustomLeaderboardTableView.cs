using TeamSaberPlugin.Helpers;
using HMUI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using static TeamSaberShared.SharedConstructs;
using Logger = TeamSaberShared.Logger;
using System;

/*
 * Created by Moon on 10/28/2018 at 2:18am
 * Intended to extend the functionality of LeaderboardTableView
 * so we can set custom colors on all the players
 */

namespace TeamSaberPlugin.UI.Views
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeaderboardTableView : MonoBehaviour, TableView.IDataSource
    {
        protected int _specialScorePos;
        protected float _rowHeight = 5f;

        private TableView _tableView;
        private LeaderboardTableCell _cellInstance;
        private List<CustomScoreData> _scores;
        private bool _useTeamColors = false;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public void Awake()
        {
            _tableView = new GameObject().AddComponent<TableView>();
            _tableView.transform.SetParent(transform, false);
            _tableView.SetField("_isInitialized", false);
            _tableView.SetField("_preallocatedCells", new TableView.CellsGroup[0]);
            _tableView.Init();

            var currentView = Resources.FindObjectsOfTypeAll<LeaderboardTableView>().First();
            var currentTransform = (currentView.transform as RectTransform);
            var newTransform = (_tableView.transform as RectTransform);

            //TODO: Wouldn't it be easier to set anchors to .5 across the board, then work from there?
            newTransform.anchorMin = new Vector2(currentTransform.anchorMin.x, currentTransform.anchorMin.y);
            newTransform.anchorMax = new Vector2(currentTransform.anchorMax.x, currentTransform.anchorMax.y);
            newTransform.anchoredPosition = new Vector2(currentTransform.anchoredPosition.x, currentTransform.anchoredPosition.y - 15); //In 0.12.0, the table was moved slightly to the right. Here I'm moving it back. Oh, and down.
            newTransform.sizeDelta = new Vector2(currentTransform.sizeDelta.x - 66, currentTransform.sizeDelta.y - 18);

            _cellInstance = Resources.FindObjectsOfTypeAll<LeaderboardTableCell>().First(x => x.name == "LeaderboardTableCell");
        }

        public void SetUpArrow(object upArrowButton)
        {
            _tableView.SetField("_pageUpButton", upArrowButton);
        }
        
        public void SetDownArrow(object downArrowButton)
        {
            _tableView.SetField("_pageDownButton", downArrowButton);
        }

        public TableCell CellForIdx(int row)
        {
            LeaderboardTableCell leaderboardTableCell = Instantiate(_cellInstance);
            leaderboardTableCell.reuseIdentifier = "Cell";

            CustomScoreData scoreData = _scores[row];
            leaderboardTableCell.rank = scoreData.rank;
            leaderboardTableCell.playerName = scoreData.playerName;
            leaderboardTableCell.score = scoreData.score;
            leaderboardTableCell.showFullCombo = scoreData.fullCombo;
            leaderboardTableCell.showSeparator = (row != _scores.Count - 1);
            leaderboardTableCell.specialScore = (_specialScorePos == row);
            if (!(_specialScorePos == row) && _useTeamColors && scoreData.TeamId != "-1") leaderboardTableCell.GetField<TextMeshProUGUI>("_playerNameText").color = Team.allTeams.FirstOrDefault(x => x.TeamId == scoreData.TeamId).Color;
            return leaderboardTableCell;
        }

        public int NumberOfCells()
        {
            if (_scores == null)
            {
                return 0;
            }
            return _scores.Count;
        }

        public float CellSize()
        {
            return _rowHeight;
        }

        public virtual void SetScores(List<CustomScoreData> scores, int specialScorePos, bool useTeamColors = false)
        {
            _scores = scores;
            _specialScorePos = specialScorePos;
            _useTeamColors = useTeamColors;
            if (_tableView.dataSource == null)
            {
                _tableView.dataSource = this;
            }
            else
            {
                _tableView.ReloadData();
            }
        }

        public class CustomScoreData : LeaderboardTableView.ScoreData
        {
            public Rarity Rarity
            {
                get;
                private set;
            }

            public string TeamId
            {
                get;
                private set;
            }

            public CustomScoreData(int score, string playerName, int place, bool fullCombo, Rarity rarity = Rarity.None, string teamId = "-1") : base(score, playerName, place, fullCombo)
            {
                Rarity = rarity;
                TeamId = teamId;
            }
        }
    }
}
