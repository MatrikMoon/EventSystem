import * as React from 'react';
import '../../style/Leaderboard.scss';
import '../../style/Button.scss';
import LeaderboardItem from './LeaderboardItem';

class Leaderboard extends React.Component {
  constructor(props) {
    super(props);
    
    this.state = {
      songList: null,
      teams: null,
      selectedSong: null,
      selectedDifficulty: null,
      selectedTeam: null,
      leaderboardData: null
    };
  }

  componentDidMount() {
    //fetch('../weeklysongs')
    fetch('/api-acc/songs/')
      .then(response => {
        return response.json();
      })
      .then(json => {
        this.setState({songList: json});
        //Help me. I have no idea how to grab a random item from this
        let item = null;
        for (item in json) {}
        if (this.state.teams != null) this.fetchLeaderboard(json[item].songHash, json[item].difficulty, "-1");
      });
    
    //fetch('../getteams')
    fetch('/api-acc/teams/')
      .then(response => {
        return response.json();
      })
      .then(json => {
        this.setState({teams: json});

        if (this.state.songList != null) {
          //Help me. I have no idea how to grab a random item from this
          let item = null;
          for (item in this.state.songList) {}  
          this.fetchLeaderboard(this.state.songList[item].songHash, this.state.songList[item].difficulty, "-1");
        }
      });
  }

  render() {
    let teamButtons = <div className="team-button-panel">
                        {this.renderTeamButtons()}
                        <button className="btn" onClick={() => this.fetchLeaderboard(null, null, "-1")}><a>Mixed</a></button>
                      </div>;
    return (
      <div className="Leaderboard transition-item">
        <header className="header">
          <span>Weekly Event Leaderboard</span>
        </header>
        <div className="page">
          <span>{`${this.state.selectedSong != null ? this.state.songList[this.state.selectedSong].songName : "Loading..."} Leaderboard`}</span>
          <div className="leaderboard-panel">
            {teamButtons}
            <div className="song-button-panel">
              {this.renderSongButtons()}
            </div> 
            <div className="leaderboard-item-panel">
              <LeaderboardItem
                key="header"
                header="true"
                place="Place"
                username="Username"
                team="Team"
                score="Score"/>
              {this.renderLeaderboardItems()}
            </div>
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
      return array.map(x => <LeaderboardItem
                              key={leaderboardData[x].place}
                              place={leaderboardData[x].place}
                              username={leaderboardData[x].player}
                              team={this.state.teams[leaderboardData[x].team] !== undefined ? this.state.teams[leaderboardData[x].team].teamName : null}
                              score={leaderboardData[x].score}
                              textColor={this.getTextColor(this.state.teams[leaderboardData[x].team] !== undefined ? this.state.teams[leaderboardData[x].team].color : null)}
                              backgroundColor={this.state.teams[leaderboardData[x].team] !== undefined ? this.state.teams[leaderboardData[x].team].color : null}
                              backgroundHighlight={this.getHighlightColor(this.state.teams[leaderboardData[x].team] !== undefined ? this.state.teams[leaderboardData[x].team].color : null)}/>
                        );
    }
  }

  renderSongButtons() {
    if (this.state.songList !== null) {
      //Must be in array form so we can .map
      //Gross time complexity, because this is in render(), I know
      let array = [];
      const songList = this.state.songList;
      for (let item in songList) array.push(item);
      return array.map(x => <button className="btn blue" key={x} onClick={() => this.fetchLeaderboard(songList[x].songHash, songList[x].difficulty, null)}><a>{songList[x].songName}</a></button>)
    }
  }

  renderTeamButtons() {
    if (this.state.teams !== null) {
      //Must be in array form so we can .map
      //Gross time complexity, because this is in render(), I know
      let array = [];
      const teams = this.state.teams;
      for (let item in teams) array.push(item);
      return array.map(x => <button className="btn" key={x} style={{'--background-color': `${teams[x].color}`, color: `${this.getTextColor(teams[x].color)}`, '--background-highlight': `${this.getHighlightColor(teams[x].color)}`}} onClick={() => this.fetchLeaderboard(null, null, x)}><a>{teams[x].teamName}</a></button>)
    }
  }

  fetchLeaderboard(song, difficulty, team) {
    let newTeam, newDifficulty, newSong;
    newTeam = (team !== null) ? team : this.state.selectedTeam;
    newDifficulty = (difficulty !== null) ? difficulty : this.state.selectedDifficulty;
    newSong = (song !== null) ? song : this.state.songList[this.state.selectedSong].songHash;
    
    //fetch('../leaderboard')
    fetch(`/api-acc/leaderboards/${newSong}/${newDifficulty}/${newTeam}/`)
      .then(response => {
        return response.json();
      })
      .then(json => {
        this.setState({leaderboardData: json, selectedSong: newSong + newDifficulty, selectedDifficulty: newDifficulty, selectedTeam: newTeam});
      });
  }

  //COLOR HELPERS
  getTextColor(color) {
    if (color == null) color = "#d50000";
    return this.shouldTextBeDarker(color) ? '#424242' : '#eeeeee';
  }

  shouldTextBeDarker(color) {
    var sum = Math.round(((parseInt(this.hexToRgb(color).r) * 299) + (parseInt(this.hexToRgb(color).g) * 587) + (parseInt(this.hexToRgb(color).b) * 114)) / 1000);
    return (sum > 128);
  }
  
  componentToHex(c) {
    var hex = c.toString(16);
    return hex.length === 1 ? "0" + hex : hex;
  }

  rgbToHex(r, g, b) {
      return "#" + this.componentToHex(r) + this.componentToHex(g) + this.componentToHex(b);
  }

  hexToRgb(hex) {
    var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
        r: parseInt(result[1], 16),
        g: parseInt(result[2], 16),
        b: parseInt(result[3], 16)
    } : null;
  }

  getHighlightColor(color) {
    if (color == null) color = "#d50000";
    return this.shouldTextBeDarker(color) ? this.shadeColor(color, -20) : this.shadeColor(color, 20);
  }

  shadeColor(color, percent) {
    var R = parseInt(color.substring(1,3),16);
    var G = parseInt(color.substring(3,5),16);
    var B = parseInt(color.substring(5,7),16);

    R = parseInt(R * (100 + percent) / 100);
    G = parseInt(G * (100 + percent) / 100);
    B = parseInt(B * (100 + percent) / 100);

    R = (R<255)?R:255;  
    G = (G<255)?G:255;  
    B = (B<255)?B:255;  

    var RR = ((R.toString(16).length === 1) ? "0"+ R.toString(16):R.toString(16));
    var GG = ((G.toString(16).length === 1) ? "0" + G.toString(16):G.toString(16));
    var BB = ((B.toString(16).length === 1) ? "0" + B.toString(16):B.toString(16));

    return "#"+RR+GG+BB;
  }
  //--End color methods
}

export default Leaderboard;
