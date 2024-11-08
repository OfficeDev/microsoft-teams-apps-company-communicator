// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as AdaptiveCards from 'adaptivecards';
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { AvatarShape } from '@fluentui/react-avatar';
import { Button, Field, Persona, Spinner, Text } from '@fluentui/react-components';
import { ArrowDownload24Regular, CheckmarkSquare24Regular, ShareScreenStop24Regular } from '@fluentui/react-icons';
import { app, dialog } from '@microsoft/teams-js';

import { exportNotification, getSentNotification } from '../../apis/messageListApi';
import { formatDate, formatDuration, formatNumber } from '../../i18n';
import { getInitAdaptiveCard, setCardAuthor, setCardBtn, setCardImageLink, setCardSummary, setCardTitle } from '../AdaptiveCard/adaptiveCard';

export interface IMessageState {
  id: string;
  title: string;
  acknowledgements?: string;
  reactions?: string;
  responses?: string;
  succeeded?: string;
  failed?: string;
  unknown?: string;
  canceled?: string;
  sentDate?: string;
  imageLink?: string;
  summary?: string;
  author?: string;
  buttonLink?: string;
  buttonTitle?: string;
  teamNames?: string[];
  rosterNames?: string[];
  groupNames?: string[];
  allUsers?: boolean;
  sendingStartedDate?: string;
  sendingDuration?: string;
  errorMessage?: string;
  warningMessage?: string;
  canDownload?: boolean;
  sendingCompleted?: boolean;
  createdBy?: string;
  isMsgDataUpdated: boolean;
}

export interface IStatusState {
  page: string;
  teamId?: string;
  isTeamDataUpdated: boolean;
}

let card: any;

export const ViewStatusTask = () => {
  const { t } = useTranslation();
  const { id } = useParams() as any;
  const [loader, setLoader] = React.useState(true);
  const [isCardReady, setIsCardReady] = React.useState(false);
  const [exportDisabled, setExportDisabled] = React.useState(false);
  const [cardAreaBorderClass, setCardAreaBorderClass] = React.useState('');

  const [messageState, setMessageState] = React.useState<IMessageState>({
    id: '',
    title: '',
    isMsgDataUpdated: false,
  });

  const [statusState, setStatusState] = React.useState<IStatusState>({
    page: 'ViewStatus',
    teamId: '',
    isTeamDataUpdated: false,
  });

  React.useEffect(() => {
    if (app.isInitialized()) {
      void app.getContext().then((context) => {
        setStatusState({ ...statusState, teamId: context.team?.internalId ?? '', isTeamDataUpdated: true });
      });
    }
  }, []);

  React.useEffect(() => {
    if (id) {
      void getMessage(id);
    }
  }, [id]);

  React.useEffect(() => {
    if (isCardReady && messageState.isMsgDataUpdated) {
      const adaptiveCard = new AdaptiveCards.AdaptiveCard();
      adaptiveCard.parse(card);
      const renderCard = adaptiveCard.render();
      if (renderCard && statusState.page === 'ViewStatus') {
        document.getElementsByClassName('card-area-1')[0].appendChild(renderCard);
        setCardAreaBorderClass('card-area-border');
      }
      adaptiveCard.onExecuteAction = function (action: any) {
        window.open(action.url, '_blank');
      };
      setLoader(false);
    }
  }, [isCardReady, messageState.isMsgDataUpdated, statusState.page]);

  const getMessage = async (id: number) => {
    try {
      await getSentNotification(id).then((response) => {
        updateCardData(response);
        response.sendingDuration = formatDuration(response.sendingStartedDate, response.sentDate);
        response.sendingStartedDate = formatDate(response.sendingStartedDate);
        response.sentDate = formatDate(response.sentDate);
        response.succeeded = formatNumber(response.succeeded);
        response.failed = formatNumber(response.failed);
        response.unknown = response.unknown && formatNumber(response.unknown);
        response.canceled = response.canceled && formatNumber(response.canceled);
        setMessageState({ ...response, isMsgDataUpdated: true });
      });
    } catch (error) {
      return error;
    }
  };

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

  const onClose = () => {
    dialog.url.submit();
  };

  const onExport = async () => {
    setExportDisabled(true);
    const payload = {
      id: messageState.id,
      teamId: statusState.teamId,
    };
    await exportNotification(payload)
      .then(() => {
        setStatusState({ ...statusState, page: 'SuccessPage' });
      })
      .catch(() => {
        setStatusState({ ...statusState, page: 'ErrorPage' });
      })
      .finally(() => {
        setExportDisabled(false);
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
    if (messageState.teamNames && messageState.teamNames.length > 0) {
      return (
        <Field size='large' label={t('SentToGeneralChannel')}>
          <ul className='ul-no-bullets'>{getItemList(messageState.teamNames, 'Team', 'square')}</ul>
        </Field>
      );
    } else if (messageState.rosterNames && messageState.rosterNames.length > 0) {
      return (
        <Field size='large' label={t('SentToRosters')}>
          <ul className='ul-no-bullets'>{getItemList(messageState.rosterNames, 'Team', 'square')}</ul>
        </Field>
      );
    } else if (messageState.groupNames && messageState.groupNames.length > 0) {
      return (
        <Field size='large' label={t('SentToGroups1')}>
          <span>{t('SentToGroups2')}</span>
          <ul className='ul-no-bullets'>{getItemList(messageState.groupNames, 'Group', 'circular')}</ul>
        </Field>
      );
    } else if (messageState.allUsers) {
      return (
        <>
          <Text size={500}>{t('SendToAllUsers')}</Text>
        </>
      );
    } else {
      return <div></div>;
    }
  };

  const renderErrorMessage = () => {
    if (messageState.errorMessage) {
      return (
        <div>
          <Field size='large' label={t('Errors')}>
            <Text className='info-text'>{messageState.errorMessage}</Text>
          </Field>
        </div>
      );
    } else {
      return <div></div>;
    }
  };

  const renderWarningMessage = () => {
    if (messageState.warningMessage) {
      return (
        <div>
          <Field size='large' label={t('Warnings')}>
            <Text className='info-text'>{messageState.warningMessage}</Text>
          </Field>
        </div>
      );
    } else {
      return <div></div>;
    }
  };

  return (
    <>
      {loader && <Spinner />}
      {statusState.page === 'ViewStatus' && (
        <>
          <span role='alert' aria-label={t('ViewMessageStatus') ?? ''} />
          <div className='adaptive-task-grid'>
            <div className='form-area'>
              {!loader && (
                <>
                  <div style={{ paddingBottom: '16px' }}>
                    <Field size='large' label={t('TitleText')}>
                      <Text style={{ overflowWrap: 'anywhere' }}>{messageState.title}</Text>
                    </Field>
                  </div>
                  <div style={{ paddingBottom: '16px' }}>
                    <Field className='spacingVerticalM' size='large' label={t('SendingStarted')}>
                      <Text>{messageState.sendingStartedDate}</Text>
                    </Field>
                  </div>
                  <div style={{ paddingBottom: '16px' }}>
                    <Field size='large' label={t('Completed')}>
                      <Text>{messageState.sentDate}</Text>
                    </Field>
                  </div>
                  <div style={{ paddingBottom: '16px' }}>
                    <Field size='large' label={t('CreatedBy')}>
                      <Persona name={messageState.createdBy} secondaryText={'Member'} avatar={{ color: 'colorful' }} />
                    </Field>
                  </div>
                  <div style={{ paddingBottom: '16px' }}>
                    <Field size='large' label={t('Duration')}>
                      <Text>{messageState.sendingDuration}</Text>
                    </Field>
                  </div>
                  <div style={{ paddingBottom: '16px' }}>
                    <Field size='large' label={t('Results')}>
                      <Text>{t('Success', { SuccessCount: messageState.succeeded })}</Text>
                      <Text>{t('Failure', { FailureCount: messageState.failed })}</Text>
                      {messageState.unknown && (
                        <>
                          <Text>{t('Unknown', { UnknownCount: messageState.unknown })}</Text>
                        </>
                      )}
                    </Field>
                  </div>
                  <div style={{ paddingBottom: '16px' }}>
                    {renderAudienceSelection()}
                    {renderErrorMessage()}
                    {renderWarningMessage()}
                  </div>
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
                {exportDisabled && <Spinner role='alert' size='small' label={t('ExportLabel')} labelPosition='after' />}
                <Button
                  icon={<ArrowDownload24Regular />}
                  style={{ marginLeft: '16px' }}
                  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                  title={exportDisabled || messageState.canDownload === false ? t('ExportButtonProgressText')! : t('ExportButtonText')!}
                  disabled={exportDisabled || messageState.canDownload === false}
                  // eslint-disable-next-line @typescript-eslint/no-misused-promises
                  onClick={onExport}
                  appearance='primary'
                >
                  {t('ExportButtonText')}
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
      {!loader && statusState.page === 'SuccessPage' && (
        <>
          <span role='alert' aria-label={t('ExportSuccessView') ?? ''} />
          <div className='dialog-padding'>
            <h2>
              <CheckmarkSquare24Regular style={{ color: '#22bb33', verticalAlign: 'top', paddingRight: '4px' }} />
              {t('ExportQueueTitle')}
            </h2>
            <Text>{t('ExportQueueSuccessMessage1')}</Text>
            <br />
            <br />
            <Text>{t('ExportQueueSuccessMessage2')}</Text>
            <br />
            <br />
            <Text>{t('ExportQueueSuccessMessage3')}</Text>
            <br />
            <br />
            <div className='fixed-footer'>
              <div className='footer-action-right'>
                <Button id='closeBtn' onClick={onClose} appearance='primary'>
                  {t('CloseText')}
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
      {!loader && statusState.page === 'ErrorPage' && (
        <>
          <span role='alert' aria-label={t('ExportFailureView') ?? ''} />
          <div className='dialog-padding'>
            <h2>
              <ShareScreenStop24Regular style={{ color: '#bb2124', verticalAlign: 'top', paddingRight: '4px' }} />
              {t('ExportErrorTitle')}
            </h2>
            <Text>{t('ExportErrorMessage')}</Text>
            <br />
            <div className='fixed-footer'>
              <div className='footer-action-right'>
                <Button id='closeBtn' onClick={onClose} appearance='primary'>
                  {t('CloseText')}
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
    </>
  );
};
