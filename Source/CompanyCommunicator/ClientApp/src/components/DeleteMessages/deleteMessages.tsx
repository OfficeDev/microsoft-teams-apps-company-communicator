// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import '../Shared/main.scss';
import './deleteMessage.scss';
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import {
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Button,
  Field,
  Radio,
  RadioGroup,
  Spinner,
  RadioGroupOnChangeData,
  Theme,
} from '@fluentui/react-components';
import { DatePicker } from '@fluentui/react-datepicker-compat';
import { ArrowLeft24Regular, CommentMultiple24Regular } from '@fluentui/react-icons';
import { app, dialog, DialogDimension, UrlDialogInfo } from '@microsoft/teams-js';
import { getBaseUrl } from '../../configVariables';
import { ROUTE_PARTS } from '../../routes';
import { RootState, useAppDispatch, useAppSelector } from '../../store';
import { Header } from '../Shared/header';
import { DeleteMessageDetail } from './deleteMessagesDetail';
import { GetDeletedMessagesAction, GetDeletedMessagesSilentAction } from '../../actions';
import * as CustomHooks from '../../useInterval';
import moment from 'moment';
import { InfoLabel } from '@fluentui/react-components/unstable';

interface IDeleteMessagesProps {
  theme: Theme;
}

export const DeleteMessages = (props: IDeleteMessagesProps) => {
  const { t } = useTranslation();
  const [fromDate, setFromDate] = React.useState<Date | undefined>();
  const [toDate, setToDate] = React.useState<Date | undefined>();
  const [deleteSelection, setDeleteSelection] = React.useState('');
  const deletedMessages = useAppSelector((state: RootState) => state.messages).deletedMessages.payload;
  const loader = useAppSelector((state: RootState) => state.messages).isDeletedMessagesFetchOn.payload;
  const dispatch = useAppDispatch();
  const delay = 10000;

  CustomHooks.useInterval(() => {
    GetDeletedMessagesSilentAction(dispatch);
  }, delay);

  React.useEffect(() => {
    if (deletedMessages && deletedMessages.length === 0) {
      GetDeletedMessagesAction(dispatch);
    }
  }, [deletedMessages]);

  const goBackToHome = () => {
    window.location.href = '/messages';
  };

  const onSelectFromDate = (date?: Date | null) => {
    if (date) {
      setFromDate(date);
    }
  };

  const onSelectToDate = (date?: Date | null) => {
    if (date) {
      setToDate(date);
    }
  };

  const deleteSelectionChange = (ev: any, data: RadioGroupOnChangeData) => {
    setDeleteSelection(data.value);
  };

  const disableApplyButton = () => {
    if (deleteSelection === '') {
      return true;
    } else if (deleteSelection === 'customDate') {
      if (toDate === undefined || fromDate === undefined) {
        return true;
      } else if (toDate < fromDate) {
        return true;
      } else if (toDate > fromDate && moment(toDate).diff(moment(fromDate), 'days') > 180) {
        return true;
      }
    }
    return false;
  };

  const getValidationErrorMsg = () => {
    if (deleteSelection === 'customDate' && fromDate && toDate && fromDate > toDate) {
      return t('invalidDateRange');
    }
    if (deleteSelection === 'customDate' && fromDate && toDate && toDate > fromDate && moment(toDate).diff(moment(fromDate), 'days') > 180) {
      return t('CustomInvalidDateRange');
    }
    return '';
  };

  const onDeleteApplyClick = () => {
    const url =
      getBaseUrl() +
      `/${ROUTE_PARTS.DELETE_MESSAGES_CONFIRM}/${deleteSelection}/${fromDate ? fromDate.toDateString() : 'NoFromDate'}/${toDate ? toDate.toDateString() : 'NoToDate'
      }`;
    const dialogInfo: UrlDialogInfo = {
      url,
      title: t('DeleteMessages') ?? '',
      size: { height: DialogDimension.Medium, width: DialogDimension.Medium },
      fallbackUrl: url,
    };

    const submitHandler: dialog.DialogSubmitHandler = (result: dialog.ISdkResponse) => {
      GetDeletedMessagesSilentAction(dispatch);
    };

    // now open the dialog
    if (app.isInitialized()) {
      dialog.url.open(dialogInfo, submitHandler);
    }
  };

  return (
    <div className='delete-messages'>
      <Header theme={props.theme} />
      <Field validationMessage={getValidationErrorMsg()} label={t('chooseRangeOfDeleteMessagesTitle')} size='large' style={{ paddingTop: '32px' }}>
        <RadioGroup onChange={deleteSelectionChange} aria-labelledby='deleteSelectionGroupLabelId'>
          <Radio value='last30Days' label={t('last30Days')} />
          <Radio value='last3Months' label={t('last3Months')} />
          <Radio value='last6Months' label={t('last6Months')} />
          <label className='customdaterangelabel'>
            <Radio value='customDate' label={t('selectACustomDate')} />
            <InfoLabel info={t('CustomDeleteInfoContent') ?? ''} />
          </label>
          {deleteSelection === 'customDate' && (
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: '160px auto',
                gridTemplateAreas: 'from-area to-area',
                columnGap: '0.5rem',
                paddingLeft: '1rem',
              }}
            >
              <Field label={t('from')} style={{ gridColumn: '1' }}>
                <DatePicker placeholder='Pick a from date' value={fromDate} style={{ maxWidth: '160px' }} onSelectDate={onSelectFromDate} maxDate={new Date()} />
              </Field>
              <Field label={t('to')} style={{ gridColumn: '2' }}>
                <DatePicker placeholder='Pick a to date' value={toDate} style={{ maxWidth: '160px' }} onSelectDate={onSelectToDate} maxDate={new Date()} />
              </Field>
            </div>
          )}
        </RadioGroup>
      </Field>
      <Button
        id='applyButtonId'
        className='cc-button'
        disabled={disableApplyButton()}
        icon={<CommentMultiple24Regular />}
        appearance='primary'
        onClick={onDeleteApplyClick}
      >
        {t('apply')}
      </Button>
      <Button id='backButtonId' className='cc-button' icon={<ArrowLeft24Regular />} appearance='secondary' onClick={goBackToHome}>
        {t('Back')}
      </Button>
      {loader && <Spinner labelPosition='below' label={t('fetching')} />}
      <Accordion defaultOpenItems='1' collapsible>
        <AccordionItem value='1' key='deleteMessagesKey'>
          <AccordionHeader>{t('DeleteMessagesSectionTitle')}</AccordionHeader>
          <AccordionPanel className='cc-accordion-panel'>
            {deletedMessages && deletedMessages.length === 0 && !loader && <div>{t('EmptyDeletedMessages')}</div>}
            {deletedMessages && deletedMessages.length > 0 && !loader && <DeleteMessageDetail deletedMessages={deletedMessages} />}
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </div>
  );
};
