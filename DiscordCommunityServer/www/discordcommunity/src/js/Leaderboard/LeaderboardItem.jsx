import React, { Component } from 'react';
import '../../style/Leaderboard.scss';
import '../../style/Button.scss';

class LeaderboardItem extends Component {
  render() {
    /*
    return (
      <div className="Leaderboard">
        <header className="item">
          <span>#{this.props.place}: {this.props.username} ({this.props.team}) - {this.props.score}</span>
        </header>
      </div>
    );
    */
    return (
      <header className="item">
        <button className="btn purple left"><span>#{this.props.place}</span></button>
        <button className="btn blue middle"><span>{this.props.username}</span></button>
        <button className="btn red middle"><span>({this.props.team})</span></button>
        <button className="btn orange right"><span>{this.props.score}</span></button>
      </header>
    );
  }
}

export default LeaderboardItem;