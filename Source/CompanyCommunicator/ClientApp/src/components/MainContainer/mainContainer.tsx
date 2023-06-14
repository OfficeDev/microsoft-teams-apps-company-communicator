// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import "./mainContainer.scss";
import * as React from "react";
import { useTranslation } from "react-i18next";
import {
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Button,
  Divider,
  Link,
  teamsLightTheme,
  Theme,
} from "@fluentui/react-components";
import { Status24Regular, PersonFeedback24Regular, QuestionCircle24Regular } from "@fluentui/react-icons";
import * as microsoftTeams from "@microsoft/teams-js";
import { GetDraftMessagesSilentAction } from "../../actions";
import mslogo from "../../assets/Images/mslogo.png";
import { getBaseUrl } from "../../configVariables";
import { ROUTE_PARTS, ROUTE_QUERY_PARAMS } from "../../routes";
import { useAppDispatch } from "../../store";
import { DraftMessages } from "../DraftMessages/draftMessages";
import { SentMessages } from "../SentMessages/sentMessages";

interface IMainContainer {
  theme: Theme;
}

export const MainContainer = (props: IMainContainer) => {
  const url = getBaseUrl() + `/${ROUTE_PARTS.NEW_MESSAGE}?${ROUTE_QUERY_PARAMS.LOCALE}={locale}`;
  const { t } = useTranslation();
  const dispatch = useAppDispatch();

  const onNewMessage = () => {
    let taskInfo: microsoftTeams.TaskInfo = {
      url,
      title: t("NewMessage"),
      height: microsoftTeams.TaskModuleDimension.Large,
      width: microsoftTeams.TaskModuleDimension.Large,
      fallbackUrl: url,
    };

    let submitHandler = (err: any, result: any) => {
      if (result === null) {
        document.getElementById("newMessageButtonId")?.focus();
      } else {
        GetDraftMessagesSilentAction(dispatch);
      }
    };

    microsoftTeams.tasks.startTask(taskInfo, submitHandler);
  };

  const customHeaderImagePath = process.env.REACT_APP_HEADERIMAGE;
  const customHeaderText = process.env.REACT_APP_HEADERTEXT
    ? t(process.env.REACT_APP_HEADERTEXT)
    : t("CompanyCommunicator");

  return (
    <>
      <div className={props.theme === teamsLightTheme ? "cc-header-light" : "cc-header"}>
        <div className="cc-main-left">
          <img
            src={customHeaderImagePath ? customHeaderImagePath : mslogo}
            alt="Microsoft logo"
            className="cc-logo"
            title={customHeaderText}
          />
          <span className="cc-title" title={customHeaderText}>
            {customHeaderText}
          </span>
        </div>
        <div className="cc-main-right">
          <span className="cc-icon-holder">
            <Link title={t("Support")} className="cc-icon-link" target="_blank" href="https://aka.ms/M365CCIssues">
              <QuestionCircle24Regular className="cc-icon" />
            </Link>
          </span>
          <span className="cc-icon-holder">
            <Link title={t("Feedback")} className="cc-icon-link" target="_blank" href="https://aka.ms/M365CCFeedback">
              <PersonFeedback24Regular className="cc-icon" />
            </Link>
          </span>
        </div>
      </div>
      <Divider />
      <div className="cc-new-message">
        <Button
          id="newMessageButtonId"
          icon={<Status24Regular />}
          appearance="primary"
          onClick={onNewMessage}
        >
          {t("NewMessage")}
        </Button>
      </div>
      <Accordion defaultOpenItems={["1", "2"]} multiple collapsible>
        <AccordionItem value="1" key="draftMessagesKey">
          <AccordionHeader>{t("DraftMessagesSectionTitle")}</AccordionHeader>
          <AccordionPanel className="cc-accordion-panel">
            <DraftMessages />
          </AccordionPanel>
        </AccordionItem>
        <AccordionItem value="2" key="sentMessagesKey">
          <AccordionHeader>{t("SentMessagesSectionTitle")}</AccordionHeader>
          <AccordionPanel className="cc-accordion-panel">
            <SentMessages />
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </>
  );
};
