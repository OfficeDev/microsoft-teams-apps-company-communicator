// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import '../Shared/main.scss';
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Accordion, AccordionHeader, AccordionItem, AccordionPanel, Button, Theme, Body1Stronger } from '@fluentui/react-components';
import { Delete24Regular, Status24Regular } from '@fluentui/react-icons';
import { app, dialog, DialogDimension, UrlDialogInfo } from '@microsoft/teams-js';
import { GetDraftMessagesSilentAction } from '../../actions';
import { getBaseUrl } from '../../configVariables';
import { ROUTE_PARTS, ROUTE_QUERY_PARAMS } from '../../routes';
import { useAppDispatch } from '../../store';
import { DraftMessages } from '../DraftMessages/draftMessages';
import { SentMessages } from '../SentMessages/sentMessages';
import { Header } from '../Shared/header';
import { ScheduledMessages } from '../ScheduledMessages/scheduledMessages';

interface IHomePage {
  theme: Theme;
}

export const HomePage = (props: IHomePage) => {
  const url = getBaseUrl() + `/${ROUTE_PARTS.NEW_MESSAGE}?${ROUTE_QUERY_PARAMS.LOCALE}={locale}`;
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [currentUser, setCurrentUser] = React.useState<string | undefined>();

  React.useEffect(() => {
    if (app.isInitialized()) {
      void app.getContext().then((context: app.Context) => {
        setCurrentUser(context.user?.userPrincipalName);
      });
    }
  }, []);

  const onNewMessage = () => {
    const dialogInfo: UrlDialogInfo = {
      url,
      title: t('NewMessage') ?? '',
      size: { height: DialogDimension.Large, width: DialogDimension.Large },
      fallbackUrl: url,
    };

    const submitHandler: dialog.DialogSubmitHandler = (result: dialog.ISdkResponse) => {
      GetDraftMessagesSilentAction(dispatch);
    };

    // now open the dialog
    if (app.isInitialized()) {
      dialog.url.open(dialogInfo, submitHandler);
    }
  };

  const onDeleteMessages = () => {
    navigate(`/${ROUTE_PARTS.DELETE_MESSAGES}`);
  };

  const hasDeletePermission = () => {
    const authorizedUsers = process.env.REACT_APP_AUTHORIZED_USERS_EMAIL;
    return currentUser ? authorizedUsers?.toLowerCase().includes(currentUser.toLowerCase()) : false;
  };

  return (
    <>
      <Header theme={props.theme} />
      <Button id='newMessageButtonId' className='cc-button' icon={<Status24Regular />} appearance='primary' onClick={onNewMessage}>
        {t('NewMessage')}
      </Button>
      {hasDeletePermission() && <Button id='deleteMessageButtonId' className='cc-button' icon={<Delete24Regular />} appearance='secondary' onClick={onDeleteMessages}>
        {t('DeleteMessages')}
      </Button>
      }
      <Accordion defaultOpenItems={['1', '2', '3']} multiple collapsible>
        <AccordionItem value='1' key='draftMessagesKey'>
          <AccordionHeader><Body1Stronger>{t('DraftMessagesSectionTitle')}</Body1Stronger></AccordionHeader>
          <AccordionPanel className='cc-accordion-panel'>
            <DraftMessages />
          </AccordionPanel>
        </AccordionItem>
        <AccordionItem value='2' key='scheduledMessagesKey'>
          <AccordionHeader>
          <Body1Stronger>{t('ScheduledMessagesSectionTitle')}</Body1Stronger>
          </AccordionHeader>
          <AccordionPanel className='cc-accordion-panel'>
            <ScheduledMessages />
          </AccordionPanel>
        </AccordionItem>
        <AccordionItem value='3' key='sentMessagesKey'>
          <AccordionHeader><Body1Stronger>{t('SentMessagesSectionTitle')}</Body1Stronger></AccordionHeader>
          <AccordionPanel className='cc-accordion-panel'>
            <SentMessages />
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </>
  );
};
