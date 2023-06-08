// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import "./signInPage.scss";
import React from "react";
import { useTranslation } from "react-i18next";
import { RouteComponentProps } from "react-router-dom";
import { Button, Text } from "@fluentui/react-components";
import * as microsoftTeams from "@microsoft/teams-js";
import i18n from "../../i18n";

const SignInPage: React.FunctionComponent<RouteComponentProps> = (props) => {
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
      },
    });
  }

  return (
    <div className="sign-in-content-container">
      <Text className="info-text" size={500}>
        {errorMessage}
      </Text>
      <div className="space"></div>
      <Button appearance="primary" className="sign-in-button" onClick={onSignIn}>
        {t("SignIn")}
      </Button>
    </div>
  );
};

export default SignInPage;
