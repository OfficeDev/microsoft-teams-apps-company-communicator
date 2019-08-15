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
import { TeamsThemeContext, getContext, ThemeStyle } from 'msteams-ui-components-react';


export interface IAppState {
  theme: string;
  themeNum: number;
}

class App extends React.Component<{}, IAppState> {

  constructor(props: {}) {
    super(props);
    this.state = {
      theme: "",
      themeNum: ThemeStyle.Light,
    }
  }

  public componentDidMount() {
    microsoftTeams.initialize();
    microsoftTeams.getContext((context) => {
      let theme = context.theme || "";
      this.updateTheme(theme);
      this.setState({
        theme: theme
      });
    });

    microsoftTeams.registerOnThemeChangeHandler((theme) => {
      this.updateTheme(theme);
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

  private updateTheme = (theme: string) => {
    if (theme === "dark") {
      this.setState({
        themeNum: ThemeStyle.Dark
      });
    } else if (theme === "contrast") {
      this.setState({
        themeNum: ThemeStyle.HighContrast
      });
    } else {
      this.setState({
        themeNum: ThemeStyle.Light
      });
    }
  }

  public getAppDom = () => {
    const context = getContext({
      baseFontSize: 10,
      style: this.state.themeNum
    });
    return (
      <TeamsThemeContext.Provider value={context}>
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
      </TeamsThemeContext.Provider>
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
