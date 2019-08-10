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
  theme: string;
}

class App extends React.Component<{}, IAppState> {
  private app: any;

  constructor(props: {}) {
    super(props);
    this.app = this;
    this.state = {
      theme: "",
    }
  }

  public componentDidMount() {
    microsoftTeams.initialize();
    microsoftTeams.getContext((context) => {
      this.app.setState({
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

  public setTheme = () => {
    if (this.state.theme === "default") {
      return (
        <Provider theme={themes.teams}>
          <div className="defaultContainer">
            {this.getAppDom()}
          </div>
        </Provider>
      );
    }
    else if (this.state.theme === "dark") {
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
        {this.setTheme()}
      </div>
    );
  }
}

export default App;
