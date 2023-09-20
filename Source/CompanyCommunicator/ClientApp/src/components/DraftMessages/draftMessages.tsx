// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { Spinner } from '@fluentui/react-components';
import { GetDraftMessagesAction } from '../../actions';
import { RootState, useAppDispatch, useAppSelector } from '../../store';
import { DraftMessageDetail } from './draftMessageDetail';

export const DraftMessages = () => {
  const { t } = useTranslation();
  const draftMessages = useAppSelector((state: RootState) => state.messages).draftMessages.payload;
  const loader = useAppSelector((state: RootState) => state.messages).isDraftMessagesFetchOn.payload;
  const dispatch = useAppDispatch();

  React.useEffect(() => {
    if (draftMessages && draftMessages.length === 0) {
      GetDraftMessagesAction(dispatch);
    }
  }, [draftMessages]);

  return (
    <>
      {loader && <Spinner labelPosition='below' label={t('fetching')} />}
      {draftMessages && draftMessages.length === 0 && !loader && <div>{t('EmptyDraftMessages')}</div>}
      {draftMessages && draftMessages.length > 0 && !loader && <DraftMessageDetail draftMessages={draftMessages} />}
    </>
  );
};
