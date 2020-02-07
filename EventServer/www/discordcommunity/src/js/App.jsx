import React, { Component } from 'react';
import { BrowserRouter as Router, Route, Switch } from "react-router-dom";
import PageTransition from 'react-router-page-transition';
import Leaderboard from './Leaderboard/Leaderboard';
import CVRELeaderboard from './Leaderboard/CVRELeaderboard';
import Homepage from './Homepage';
import '../style/index.scss'

class App extends Component {
  render() {
    return (
        <Router>
            <Route
                render={({ location }) => (
                    <PageTransition timeout={500}>
                        <Switch location={location}>
                            <Route path="/leaderboard/" component={Leaderboard} />
                            <Route path="/cvre-leaderboard/" component={CVRELeaderboard} />
                            <Route path="/" component={Homepage}/>
                        </Switch>
                    </PageTransition>
                )}
            />
        </Router>
    );
  }
}

export default App;