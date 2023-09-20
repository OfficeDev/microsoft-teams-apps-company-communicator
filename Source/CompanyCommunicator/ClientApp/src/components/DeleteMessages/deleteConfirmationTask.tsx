// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { Button, Caption1Stronger, Text, Body1Stronger, Spinner } from '@fluentui/react-components';
import { dialog } from '@microsoft/teams-js';
import { deleteMessages } from '../../apis/messageListApi';
import { useAppDispatch } from '../../store';
import { GetDeletedMessagesSilentAction } from '../../actions';
import { IDeleteMessageRequest } from '../../models/deleteMessages';
import moment from 'moment';

export const DeleteConfirmationTask = () => {
  const { deletionType, deletionFromDate, deletionToDate } = useParams() as any;
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const [showDeletingSpinner, setShowDeletingSpinner] = React.useState(false);

  const onBack = () => {
    dialog.url.submit();
  };

  const setDelay = () => {
    setShowDeletingSpinner(false);
    dialog.url.submit();
  };

  const onDelete = () => {
    let fromDate = moment().format('MM/DD/YYYY');
    let toDate = moment().format('MM/DD/YYYY');

    if (deletionType.toLowerCase() === 'customdate') {
      fromDate = moment(deletionFromDate).format('MM/DD/YYYY');
      toDate = moment(deletionToDate).format('MM/DD/YYYY');
    } else if (deletionType.toLowerCase() === 'last30days') {
      fromDate = moment().subtract(30, 'days').format('MM/DD/YYYY');
    } else if (deletionType.toLowerCase() === 'last3months') {
      fromDate = moment().subtract(90, 'days').format('MM/DD/YYYY');
    } else if (deletionType.toLowerCase() === 'last6months') {
      fromDate = moment().subtract(180, 'days').format('MM/DD/YYYY');
    }
    setShowDeletingSpinner(true);

    const payload: IDeleteMessageRequest = { selectedDateRange: deletionType, startDate: fromDate, endDate: toDate };
    deleteHistoricalMessages(payload);
  };

  const deleteHistoricalMessages = (payload: IDeleteMessageRequest) => {
    try {
      deleteMessages(payload)
        .then(() => {
          GetDeletedMessagesSilentAction(dispatch);
        })
        .finally(() => {
          setTimeout(setDelay, 5000);
        });
    } catch (error) {
      return error;
    }
  };

  return (
    <>
      <Body1Stronger>{t('deleteTheMessages')}</Body1Stronger>
      <br />
      <br />
      <Caption1Stronger>{t('dateRange')}</Caption1Stronger>
      <br />
      {deletionType.toLowerCase() === 'last30days' && <Text>{t('last30Days')}</Text>}
      {deletionType.toLowerCase() === 'last3months' && <Text>{t('last3Months')}</Text>}
      {deletionType.toLowerCase() === 'last6months' && <Text>{t('last6Months')}</Text>}
      {deletionType.toLowerCase() === 'customdate' && (
        <Text>
          {t('from')}&nbsp;{deletionFromDate}&nbsp;{t('to')}&nbsp;{deletionToDate}
        </Text>
      )}
      <br />
      <br />
      <Text className='info-text'>{t('deleteConfirmationNote')}</Text>
      <br />
      <br />
      <div className='fixed-footer'>
        <div className='footer-action-right'>
          <div className='footer-actions-flex'>
            {showDeletingSpinner && (
                    <Spinner
                      role='alert'
                      id='deletingLoader'
                      size='small'
                      label={t('DeletingMessagesLabel')}
                      labelPosition='after'
                    />
            )}
            <Button onClick={onBack} style={{ marginLeft: '16px' }} appearance='secondary' disabled={showDeletingSpinner} >
              {t('Back')}
            </Button>
            <Button onClick={onDelete} style={{ marginLeft: '16px' }} appearance='primary' disabled={showDeletingSpinner}>
              {t('delete')}
            </Button>
          </div>
        </div>
      </div>
    </>
  );
};
