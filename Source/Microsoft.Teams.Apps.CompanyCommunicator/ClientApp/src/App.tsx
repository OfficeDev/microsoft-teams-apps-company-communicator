import React from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import Configuration from './components/config';
import TabContainer from './components/TabContainer/tabContainer';
import NewMessage from './components/NewMessage/newMessage';
import './App.scss';

class App extends React.Component {
  public render() {
    return (
      <div className="appContainer">
        <BrowserRouter>
          <Switch>
            <Route exact path="/configtab" component={Configuration} />
            <Route exact path="/messages" component={TabContainer} />
            <Route exact path="/newmessage" component={NewMessage} />
          </Switch>
        </BrowserRouter>
      </div>
    );
  }
}

export default App;
