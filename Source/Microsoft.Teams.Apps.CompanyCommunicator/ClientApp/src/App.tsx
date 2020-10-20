import React, { Suspense } from 'react';
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
import ErrorPage from "./components/ErrorPage/errorPage";
import SignInPage from "./components/SignInPage/signInPage";
import SignInSimpleStart from "./components/SignInPage/signInSimpleStart";
import SignInSimpleEnd from "./components/SignInPage/signInSimpleEnd";
import { updateLocale } from './i18n';

export interface IAppState {
  theme: string;
  themeStyle: number;
}

class App extends React.Component<{}, IAppState> {

  constructor(props: {}) {
    super(props);
    this.state = {
      theme: "",
      themeStyle: ThemeStyle.Light,
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

    updateLocale();
  }

  public setThemeComponent = () => {
    if (this.state.theme === "dark") {
      return (
        <Provider theme={themes.teamsDark}>
          <div className="darkContainer">
            {this.getAppDom()}
          </div>
        </Provider>
      );
    }
    else if (this.state.theme === "contrast") {
      return (
        <Provider theme={themes.teamsHighContrast}>
          <div className="highContrastContainer">
            {this.getAppDom()}
          </div>
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
        themeStyle: ThemeStyle.Dark
      });
    } else if (theme === "contrast") {
      this.setState({
        themeStyle: ThemeStyle.HighContrast
      });
    } else {
      this.setState({
        themeStyle: ThemeStyle.Light
      });
    }
  }

  public getAppDom = () => {
    const context = getContext({
      baseFontSize: 10,
      style: this.state.themeStyle
    });
    return (
      <TeamsThemeContext.Provider value={context}>
        <Suspense fallback={<div></div>}>
          <div className="appContainer">
            <BrowserRouter>
              <Switch>
                <Route exact path="/configtab" component={Configuration} />
                <Route exact path="/messages" component={TabContainer} />
                <Route exact path="/newmessage" component={NewMessage} />
                <Route exact path="/newmessage/:id" component={NewMessage} />
                <Route exact path="/viewstatus/:id" component={StatusTaskModule} />
                <Route exact path="/sendconfirmation/:id" component={SendConfirmationTaskModule} />
                <Route exact path="/errorpage" component={ErrorPage} />
                <Route exact path="/errorpage/:id" component={ErrorPage} />
                <Route exact path="/signin" component={SignInPage} />
                <Route exact path="/signin-simple-start" component={SignInSimpleStart} />
                <Route exact path="/signin-simple-end" component={SignInSimpleEnd} />
              </Switch>
            </BrowserRouter>
          </div>
        </Suspense>
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