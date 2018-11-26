import React, { Component } from 'react';
import '../../style/Leaderboard.scss';

class LeaderboardItem extends Component {
  render() {
    return (
      <div className="Leaderboard">
        <header className="item">
          <span>#{this.props.place}: {this.props.username} ({this.props.rank}) - {this.props.score}</span>
        </header>
      </div>
    );
  }
}

export default LeaderboardItem;