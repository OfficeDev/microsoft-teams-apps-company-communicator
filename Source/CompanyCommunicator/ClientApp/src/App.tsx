import React from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import ListControl from './components/ListControl/listControl';
import Configuration from './components/config';
import './App.css';

class App extends React.Component {
  render() {
    return (
      <div>
        <BrowserRouter>
          <Switch>
            <Route exact path="/tabs" component={Configuration} />
            <Route exact path="/list" component={ListControl} />
          </Switch>
        </BrowserRouter>
      </div>
    );
  }
}

export default App;
