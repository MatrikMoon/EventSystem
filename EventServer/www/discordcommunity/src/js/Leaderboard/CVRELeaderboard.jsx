import * as React from 'react';
import '../../style/Leaderboard.scss';
import '../../style/Button.scss';
import LeaderboardItem from './LeaderboardItem';

class CVRELeaderboard extends React.Component {
  constructor(props) {
    super(props);
    
    this.state = {
      songList: [],
      teams: [],
      selectedSong: null,
      selectedTeam: null,
      rawData: null
    };
  }

  componentDidMount() {
    const allowedSongs = ["custom_level_D0B25B0B05C7B14C5EA55774D1F751705F360026", "custom_level_AB36FD22C1060AC3696AF0CD6BD1E25DB3AEBF18", "custom_level_B3BD1BBBB18A53381E183EEADF78CCC5E523F07D"];
    
    fetch('https://cvrescores.herokuapp.com/api/scores')
      .then(response => {
        return response.json();
      })
      .then(json => {
        this.setState({rawData: json});

        let item = null;
        for (item in json) {
          item = json[item];
          if ((!this.state.teams || (this.state.teams && !this.state.teams.includes(item.team))) && item.team) {
            this.state.teams[item.team] = {
              "teamName" : item.team,
              "color" : "#2ecc71"
            };
          }
          if ((!this.state.songList || (this.state.songList && !this.state.songList.includes(item.levelId))) && allowedSongs.includes(item.levelId)) {
            this.state.songList[item.levelId] = {
              "songName" : item.mapName,
              "songHash" : item.levelId
            };
          }
        }
        this.fetchLeaderboard(json['0'].levelId, null);
      });
  }

  render() {
    let teamButtons = <div className="team-button-panel">
                        {this.renderTeamButtons()}
                        <button className="btn" onClick={() => this.fetchLeaderboard(null, "-1")}><a>Mixed</a></button>
                      </div>;
    return (
      <div className="Leaderboard transition-item">
        <header className="header">
          <span>CVRE Leaderboard</span>
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
    if (this.state.selectedSong === null) {
      return (
        <div>
          Leaderboard error
        </div>
      );
    }
    else {
      //Must be in array form so we can .map
      //Gross time complexity, because this is in render(), I know
      let scoresForThisSong = [];
      const leaderboardData = this.state.rawData;
      leaderboardData.sort((a, b) => {
        return b.score - a.score;
      });

      for (let item in leaderboardData) {
        let score = leaderboardData[item];
        if (score.levelId == this.state.selectedSong && !scoresForThisSong.some(x => x.player == score.username)
          && ((!this.state.selectedTeam || this.state.selectedTeam == "-1") || this.state.selectedTeam == score.team)) {
          scoresForThisSong[scoresForThisSong.length] = {
            "place" : scoresForThisSong.length + 1,
            "player" : score.username,
            "score" : `${score.score} (${parseFloat(score.accuracy) * 100}%)`,
            "team" : score.team
          };
        }
      }

      return scoresForThisSong.map(x => <LeaderboardItem
                              key={x.place}
                              place={x.place}
                              username={x.player}
                              team={this.state.teams[x.team] !== undefined ? this.state.teams[x.team].teamName : null}
                              score={x.score}
                              textColor={this.getTextColor(this.state.teams[x.team] !== undefined ? this.state.teams[x.team].color : null)}
                              backgroundColor={this.state.teams[x.team] !== undefined ? this.state.teams[x.team].color : null}
                              backgroundHighlight={this.getHighlightColor(this.state.teams[x.team] !== undefined ? this.state.teams[x.team].color : null)}/>
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
      return array.map(x => <button className="btn blue" key={x} onClick={() => this.fetchLeaderboard(songList[x].songHash, null)}><a>{songList[x].songName}</a></button>)
    }
  }

  renderTeamButtons() {
    if (this.state.teams !== null) {
      //Must be in array form so we can .map
      //Gross time complexity, because this is in render(), I know
      let array = [];
      const teams = this.state.teams;
      for (let item in teams) array.push(item);
      return array.map(x => <button className="btn" key={x} style={{'--background-color': `${teams[x].color}`, color: `${this.getTextColor(teams[x].color)}`, '--background-highlight': `${this.getHighlightColor(teams[x].color)}`}} onClick={() => this.fetchLeaderboard(null, x)}><a>{teams[x].teamName}</a></button>)
    }
  }

  fetchLeaderboard(song, team) {
    let newTeam, newSong;
    newTeam = (team !== null) ? team : this.state.selectedTeam;
    newSong = (song !== null) ? song : this.state.songList[this.state.selectedSong].songHash;
    
    this.setState({selectedSong: newSong, selectedTeam: newTeam});
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

export default CVRELeaderboard;
