import * as React from 'react';
import '../../style/Leaderboard.scss';
import LeaderboardItem from './LeaderboardItem';

class Leaderboard extends React.Component {
  constructor(props) {
    super(props);
    
    this.state = {
      songList: null,
      selectedSong: null,
      selectedRank: null,
      leaderboardData: null
    };
  }

  componentDidMount() {
    //fetch('../weeklysongs')
    fetch('/api/getweeklysongs/')
      .then(response => {
        return response.json();
      })
      .then(json => {
        this.setState({songList: json});
        //Help me. I have no idea how to grab a random item from this
        let item = null;
        for (item in json) {}
        this.fetchLeaderboard(item, 6);
      });
  }

  render() {
    return (
      <div className="Leaderboard transition-item">
        <header className="header">
          <span>Discord Community Leaderboard</span>
        </header>
        <div className="page">
          <span>{`${this.getRankText(this.state.selectedRank)} Leaderboard`}</span>
          <div className="leaderboard-panel">
            <div className="song-button-panel">
              {this.renderSongButtons()}
            </div>
            <div className="rank-button-panel">
            <button class="btn white" onClick={() => this.fetchLeaderboard(null, 0)}><a>White</a></button>
            <button class="btn brown" onClick={() => this.fetchLeaderboard(null, 1)}><a>Bronze</a></button>
            <button class="btn gray" onClick={() => this.fetchLeaderboard(null, 2)}><a>Silver</a></button>
            <button class="btn yellow" onClick={() => this.fetchLeaderboard(null, 3)}><a>Gold</a></button>
            <button class="btn blue" onClick={() => this.fetchLeaderboard(null, 4)}><a>Blue</a></button>
            <button class="btn purple" onClick={() => this.fetchLeaderboard(null, 5)}><a>Master</a></button>
            <button class="btn" onClick={() => this.fetchLeaderboard(null, 6)}><a>Mixed</a></button>
            </div>
            {this.renderLeaderboardItems()}
          </div>
        </div>
      </div>
    );
  }

  renderLeaderboardItems() {
    if (this.state.leaderboardData === null) {
      return (
        <div>
          Leaderboard error
        </div>
      );
    }
    else {
      //Must be in array form so we can .map
      //Gross time complexity, because this is in render(), I know
      let array = [];
      const leaderboardData = this.state.leaderboardData;
      for (let item in leaderboardData) array.push(item);
      return array.map(x => <LeaderboardItem key={leaderboardData[x].place} place={leaderboardData[x].place} username={leaderboardData[x].player} rank={this.getRankText(leaderboardData[x].rank)} score={leaderboardData[x].score}/>);
    }
  }

  renderSongButtons() {
    if (this.state.songList !== null) {
      //Must be in array form so we can .map
      //Gross time complexity, because this is in render(), I know
      let array = [];
      const songList = this.state.songList;
      for (let item in songList) array.push(item);
      return array.map(x => <button className="btn blue" onClick={() => this.fetchLeaderboard(x, null)}><a>{songList[x].songName}</a></button>)
    }
  }

  fetchLeaderboard(song, rank) {
    let newRank, newSong;
    newRank = (rank !== null) ? rank : this.state.selectedRank;
    newSong = (song !== null) ? song : this.state.selectedSong;
    
    //fetch('../leaderboard')
    fetch(`/api/getsongleaderboards/${newSong}/${newRank}/0`)
      .then(response => {
        return response.json();
      })
      .then(json => {
        this.setState({leaderboardData: json, selectedSong: newSong, selectedRank: newRank});
      });
  }

  getRankText(rank) {
    switch (rank) {
      case 0:
        return "White";
      case 1:
        return "Bronze";
      case 2:
        return "Silver";
      case 3:
        return "Gold";
      case 4:
        return "Blue";
      case 5:
        return "Master";
      case 6:
        return "Mixed";
    }
  }
}

export default Leaderboard;
