// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as AdaptiveCards from 'adaptivecards';
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { Button, Field, Label, Persona, Spinner, Text } from '@fluentui/react-components';
import { dialog } from '@microsoft/teams-js';
import { getConsentSummaries, getDraftNotification, sendDraftNotification } from '../../apis/messageListApi';
import { getInitAdaptiveCard, setCardAuthor, setCardBtn, setCardImageLink, setCardSummary, setCardTitle } from '../AdaptiveCard/adaptiveCard';
import { AvatarShape } from '@fluentui/react-avatar';

export interface IMessageState {
  id: string;
  title: string;
  acknowledgements?: number;
  reactions?: number;
  responses?: number;
  succeeded?: number;
  failed?: number;
  throttled?: number;
  sentDate?: string;
  imageLink?: string;
  summary?: string;
  author?: string;
  buttonLink?: string;
  buttonTitle?: string;
  createdBy?: string;
  isDraftMsgUpdated: boolean;
}

export interface IConsentState {
  teamNames: string[];
  rosterNames: string[];
  groupNames: string[];
  allUsers: boolean;
  messageId: number;
  isConsentsUpdated: boolean;
}

let card: any;

export const SendConfirmationTask = () => {
  const { t } = useTranslation();
  const { id } = useParams() as any;
  const [loader, setLoader] = React.useState(true);
  const [isCardReady, setIsCardReady] = React.useState(false);
  const [disableSendButton, setDisableSendButton] = React.useState(false);
  const [cardAreaBorderClass, setCardAreaBorderClass] = React.useState('');

  const [messageState, setMessageState] = React.useState<IMessageState>({
    id: '',
    title: '',
    isDraftMsgUpdated: false,
  });

  const [consentState, setConsentState] = React.useState<IConsentState>({
    teamNames: [],
    rosterNames: [],
    groupNames: [],
    allUsers: false,
    messageId: 0,
    isConsentsUpdated: false,
  });

  React.useEffect(() => {
    if (id) {
      void getDraftMessage(id);
      void getConsents(id);
    }
  }, [id]);

  React.useEffect(() => {
    if (isCardReady && consentState.isConsentsUpdated && messageState.isDraftMsgUpdated) {
      const adaptiveCard = new AdaptiveCards.AdaptiveCard();
      adaptiveCard.parse(card);
      const renderCard = adaptiveCard.render();
      if (renderCard) {
        document.getElementsByClassName('card-area-1')[0].appendChild(renderCard);
        setCardAreaBorderClass('card-area-border');
      }
      adaptiveCard.onExecuteAction = function (action: any) {
        window.open(action.url, '_blank');
      };
      setLoader(false);
    }
  }, [isCardReady, consentState.isConsentsUpdated, messageState.isDraftMsgUpdated]);

  const updateCardData = (msg: IMessageState) => {
    card = getInitAdaptiveCard(msg.title);
    setCardTitle(card, msg.title);
    setCardImageLink(card, msg.imageLink);
    setCardSummary(card, msg.summary);
    setCardAuthor(card, msg.author);
    if (msg.buttonTitle && msg.buttonLink) {
      setCardBtn(card, msg.buttonTitle, msg.buttonLink);
    }
    setIsCardReady(true);
  };

  const getDraftMessage = async (id: number) => {
    try {
      await getDraftNotification(id).then((response) => {
        updateCardData(response);
        setMessageState({ ...response, isDraftMsgUpdated: true });
      });
    } catch (error) {
      return error;
    }
  };

  const getConsents = async (id: number) => {
    try {
      await getConsentSummaries(id).then((response) => {
        setConsentState({
          ...consentState,
          teamNames: response.teamNames.sort(),
          rosterNames: response.rosterNames.sort(),
          groupNames: response.groupNames.sort(),
          allUsers: response.allUsers,
          messageId: id,
          isConsentsUpdated: true,
        });
      });
    } catch (error) {
      return error;
    }
  };

  const onSendMessage = () => {
    setDisableSendButton(true);
    sendDraftNotification(messageState)
      .then(() => {
        dialog.url.submit();
      })
      .finally(() => {
        setDisableSendButton(false);
      });
  };

  const getItemList = (items: string[], secondaryText: string, shape: AvatarShape) => {
    const resultedTeams: any[] = [];
    if (items) {
      // eslint-disable-next-line array-callback-return
      items.map((element) => {
        resultedTeams.push(
          <li key={element + 'key'}>
            <Persona name={element} secondaryText={secondaryText} avatar={{ shape, color: 'colorful' }} />
          </li>
        );
      });
    }
    return resultedTeams;
  };

  const renderAudienceSelection = () => {
    if (consentState.teamNames && consentState.teamNames.length > 0) {
      return (
        <div key='teamNames' style={{ paddingBottom: '16px' }}>
          <Label>{t('TeamsLabel')}</Label>
          <ul className='ul-no-bullets'>{getItemList(consentState.teamNames, 'Team', 'square')}</ul>
        </div>
      );
    } else if (consentState.rosterNames && consentState.rosterNames.length > 0) {
      return (
        <div key='rosterNames' style={{ paddingBottom: '16px' }}>
          <Label>{t('TeamsMembersLabel')}</Label>
          <ul className='ul-no-bullets'>{getItemList(consentState.rosterNames, 'Team', 'square')}</ul>
        </div>
      );
    } else if (consentState.groupNames && consentState.groupNames.length > 0) {
      return (
        <div key='groupNames' style={{ paddingBottom: '16px' }}>
          <Label>{t('GroupsMembersLabel')}</Label>
          <ul className='ul-no-bullets'>{getItemList(consentState.groupNames, 'Group', 'circular')}</ul>
        </div>
      );
    } else if (consentState.allUsers) {
      return (
        <div key='allUsers' style={{ paddingBottom: '16px' }}>
          <Label>{t('AllUsersLabel')}</Label>
          <div>
            <Text className='info-text'>{t('SendToAllUsersNote')}</Text>
          </div>
        </div>
      );
    } else {
      return <div></div>;
    }
  };

  return (
    <>
      {loader && <Spinner />}
      <>
        <div className='adaptive-task-grid'>
          <div className='form-area'>
            {!loader && (
              <>
                <div style={{ paddingBottom: '16px' }}>
                  <Field size='large' label={t('ConfirmToSend')}>
                    <Text>{t('SendToRecipientsLabel')}</Text>
                  </Field>
                </div>
                <div>{renderAudienceSelection()}</div>
              </>
            )}
          </div>
          <div className='card-area'>
            <div className={cardAreaBorderClass}>
              <div className='card-area-1'></div>
            </div>
          </div>
        </div>
        <div className='fixed-footer'>
          <div className='footer-action-right'>
            <div className='footer-actions-flex'>
              {disableSendButton && <Spinner role='alert' id='sendLoader' label={t('PreparingMessageLabel')} size='small' labelPosition='after' />}
              <Button disabled={loader || disableSendButton} style={{ marginLeft: '16px' }} onClick={onSendMessage} appearance='primary'>
                {t('Send')}
              </Button>
            </div>
          </div>
        </div>
      </>
    </>
  );
};
