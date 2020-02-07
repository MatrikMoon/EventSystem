import React, { Component } from 'react';
import { Link } from "react-router-dom";
import '../style/Homepage.scss';

class Homepage extends Component {
  handleClick = () => {
    this.props.history.push('/leaderboard/');
  };

  handleCVREClick = () => {
    this.props.history.push('/cvre-leaderboard/');
  };

  render() {
    return (
      <div className="Homepage transition-item">
        <header className="header">
            <div className="logo">
                <img src={require('../style/moonmoonlayer.png')} className="moonlogo" alt="logo" />
                <img src={require('../style/moonstarlayer.png')} className="starslogo" alt="logo" />
            </div>
            <button className="btn green" onClick={this.handleClick}><span>Event Leaderboards</span></button>
            <button className="btn blue" onClick={this.handleCVREClick}><span>CVRE Leaderboards</span></button>
            <button className="btn orange" onClick={() => window.location="../casino/middleman.php"}><span>Kik Bot Captcha Checker</span></button>
            <button className="btn red" onClick={() => window.location="https://www.google.com"}><span>Google</span></button>
        </header>
      </div>
    );
  }
}

export default Homepage;