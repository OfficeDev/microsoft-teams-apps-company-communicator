// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import "./errorPage.scss";
import React from "react";
import { useTranslation } from "react-i18next";
import { RouteComponentProps } from "react-router-dom";

import { Text } from "@fluentui/react-components";

const ErrorPage: React.FunctionComponent<RouteComponentProps> = (props) => {
  const { t } = useTranslation();

  function parseErrorMessage(): string {
    const params = props.match.params;
    if ("id" in params) {
      const id = params["id"];
      if (id === "401") {
        return t("UnauthorizedErrorMessage");
      } else if (id === "403") {
        return t("ForbiddenErrorMessage");
      }
    }
    return t("GeneralErrorMessage");
  }

  return (
    <Text className="error-message" size={500}>
      {parseErrorMessage()}
    </Text>
  );
};

export default ErrorPage;
