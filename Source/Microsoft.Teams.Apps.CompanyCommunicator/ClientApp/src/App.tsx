import React from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import Configuration from './components/config';
import TabContainer from './components/TabContainer/tabContainer';
import NewMessage from './components/NewMessage/newMessage';
import StatusTaskModule from './components/StatusTaskModule/statusTaskModule';
import './App.scss';
import { Provider, themes } from '@stardust-ui/react';
import SendConfirmationTaskModule from './components/SendConfirmationTaskModule/sendConfirmationTaskModule';
import * as microsoftTeams from "@microsoft/teams-js";

export interface IAppState {
  theme: string | undefined;
}

class App extends React.Component<{}, IAppState> {

  constructor(props: {}) {
    super(props);
    this.state = {
      theme: "",
    }
  }

  public componentDidMount() {
    microsoftTeams.initialize();
    microsoftTeams.getContext((context) => {
      this.setState({
        theme: context.theme
      });
    });

    microsoftTeams.registerOnThemeChangeHandler((theme) => {
      this.setState({
        theme: theme,
      }, () => {
        this.forceUpdate();
      });
    });
  }

  public setThemeComponent = () => {
    if (this.state.theme === "dark") {
      return (
        <Provider theme={themes.teamsDark}>
          {this.getAppDom()}
        </Provider>
      );
    }
    else if (this.state.theme === "contrast") {
      return (
        <Provider theme={themes.teamsHighContrast}>
          {this.getAppDom()}
        </Provider>
      );
    } else {
      return (
        <Provider theme={themes.teams}>
          <div className="defaultContainer">
            {this.getAppDom()}
          </div>
        </Provider>
      );
    }
  }

  public getAppDom = () => {
    return (
      <div className="appContainer">
        <BrowserRouter>
          <Switch>
            <Route exact path="/configtab" component={Configuration} />
            <Route exact path="/messages" component={TabContainer} />
            <Route exact path="/newmessage" component={NewMessage} />
            <Route exact path="/newmessage/:id" component={NewMessage} />
            <Route exact path="/viewstatus/:id" component={StatusTaskModule} />
            <Route exact path="/sendconfirmation/:id" component={SendConfirmationTaskModule} />
          </Switch>
        </BrowserRouter>
      </div>
    );
  }

  public render(): JSX.Element {
    return (
      <div>
        {this.setThemeComponent()}
      </div>
    );
  }
}

export default App;
