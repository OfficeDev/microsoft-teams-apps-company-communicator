import React from "react";
import { RouteComponentProps } from "react-router-dom";
import { useTranslation } from 'react-i18next';
import { Text, Button } from "@stardust-ui/react";
import * as microsoftTeams from "@microsoft/teams-js";
import "./signInPage.scss";
import i18n from "../../i18n";

const SignInPage: React.FunctionComponent<RouteComponentProps> = props => {
  const { t } = useTranslation();
  const errorMessage = t("SignInPromptMessage");

  function onSignIn() {
    microsoftTeams.authentication.authenticate({
      url: window.location.origin + "/signin-simple-start",
      successCallback: () => {
        console.log("Login succeeded!");
        window.location.href = "/messages";
      },
      failureCallback: (reason) => {
        console.log("Login failed: " + reason);
        window.location.href = `/errorpage?locale=${i18n.language}`;
      }
    });
  }

  return (
    <div className="sign-in-content-container">
      <Text
        content={errorMessage}
        size="medium"
      />
      <div className="space"></div>
      <Button content={t("SignIn")} primary className="sign-in-button" onClick={onSignIn} />
    </div>
  );
};

export default SignInPage;
