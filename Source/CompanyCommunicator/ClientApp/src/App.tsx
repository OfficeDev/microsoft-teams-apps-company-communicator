import React from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import Configuration from './components/config';
import tabContainer from './components/TabContainer/tabContainer';
import NewMessage from './components/NewMessage/newMessage';
import './App.scss';

class App extends React.Component {
  render() {
    return (
      <div>
        <BrowserRouter>
          <Switch>
            <Route exact path="/configtab" component={Configuration} />
            <Route exact path="/messages" component={tabContainer} />
            <Route exact path="/newmessage" component={NewMessage} />
          </Switch>
        </BrowserRouter>
      </div>
    );
  }
}

export default App;
