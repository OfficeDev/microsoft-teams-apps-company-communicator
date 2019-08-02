import React from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import Configuration from './components/config';
import TabContainer from './components/TabContainer/tabContainer';
import NewMessage from './components/NewMessage/newMessage';
import StatusTaskModule from './components/StatusTaskModule/statusTaskModule';
import './App.scss';
import { Provider, themes } from '@stardust-ui/react';
import ConfirmationTaskModule from './components/ConfirmationTaskModule/confirmationTaskModule';

class App extends React.Component {
  public render(): JSX.Element {
    return (
      <Provider theme={themes.teams}>
        <div className="appContainer">
          <BrowserRouter>
            <Switch>
              <Route exact path="/configtab" component={Configuration} />
              <Route exact path="/messages" component={TabContainer} />
              <Route exact path="/newmessage" component={NewMessage} />
              <Route exact path="/newmessage/:id" component={NewMessage} />
              <Route exact path="/viewstatus/:id" component={StatusTaskModule} />
              <Route exact path="/confirmation/:id" component={ConfirmationTaskModule} />
            </Switch>
          </BrowserRouter>
        </div>
      </Provider>
    );
  }
}

export default App;
