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
    
    let teamButton = <button className="btn red middle"><span>{this.props.team}</span></button>;
    if (this.props.backgroundColor != null && this.props.backgroundHighlight != null && this.props.textColor != null) {
      teamButton = <button className="btn middle" style={{'--background-color': `${this.props.backgroundColor}`, color: `${this.props.textColor}`, '--background-highlight': `${this.props.backgroundHighlight}`}}><span>{this.props.team}</span></button>;
    }

    return (
      <header className={this.props.header === "true" ? "item titlebar" : "item"}>
        <button className="btn purple left"><span>#{this.props.place}</span></button>
        <button className="btn blue middle"><span>{this.props.username}</span></button>
        {teamButton}
        <button className="btn orange right"><span>{this.props.score}</span></button>
      </header>
    );
  }
}

export default LeaderboardItem;