// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from 'react';
import { useTranslation } from 'react-i18next';
import {
  Button,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Table,
  TableBody,
  TableCell,
  TableCellLayout,
  TableHeader,
  TableHeaderCell,
  TableRow,
  useArrowNavigationGroup,
  Body1Strong,
} from '@fluentui/react-components';
import {
  DeleteRegular,
  DocumentCopyRegular,
  Chat20Regular,
  EditRegular,
  MoreHorizontal24Filled,
  OpenRegular,
  SendRegular,
} from '@fluentui/react-icons';
import { getBaseUrl } from '../../configVariables';
import { ROUTE_PARTS, ROUTE_QUERY_PARAMS } from '../../routes';
import { useAppDispatch } from '../../store';
import { app, dialog, DialogDimension, UrlDialogInfo } from '@microsoft/teams-js';
import {
  GetDraftMessagesSilentAction,
  GetScheduledMessagesSilentAction,
  GetSentMessagesSilentAction,
} from '../../actions';
import { deleteDraftNotification, duplicateDraftNotification, sendPreview } from '../../apis/messageListApi';
import { formatDate } from '../../i18n';

export const ScheduledMessageDetail = (scheduledMessages: any) => {
  const { t } = useTranslation();
  const keyboardNavAttr = useArrowNavigationGroup({ axis: 'grid' });
  const [teamsTeamId, setTeamsTeamId] = React.useState('');
  const [teamsChannelId, setTeamsChannelId] = React.useState('');
  const dispatch = useAppDispatch();
  const sendUrl = (id: string) =>
    getBaseUrl() + `/${ROUTE_PARTS.SEND_CONFIRMATION}/${id}?${ROUTE_QUERY_PARAMS.LOCALE}={locale}`;
  const editUrl = (id: string) =>
    getBaseUrl() + `/${ROUTE_PARTS.NEW_MESSAGE}/${id}?${ROUTE_QUERY_PARAMS.LOCALE}={locale}`;
  const previewConfirmationUrl = () =>
    getBaseUrl() + `/${ROUTE_PARTS.PREVIEW_MESSAGE_CONFIRMATION}?${ROUTE_QUERY_PARAMS.LOCALE}={locale}`;

  React.useEffect(() => {
    if (app.isInitialized()) {
      void app.getContext().then((context: app.Context) => {
        setTeamsTeamId(context.team?.internalId ?? '');
        setTeamsChannelId(context.channel?.id ?? '');
      });
    }
  }, []);

  const onOpenTaskModule = (url: string, title: string) => {
    const dialogInfo: UrlDialogInfo = {
      url,
      title,
      size: { height: DialogDimension.Large, width: DialogDimension.Large },
      fallbackUrl: url,
    };

    const submitHandler: dialog.DialogSubmitHandler = (result: dialog.ISdkResponse) => {
      GetScheduledMessagesSilentAction(dispatch);
      GetDraftMessagesSilentAction(dispatch);
      GetSentMessagesSilentAction(dispatch);
    };
    // now open the dialog
    dialog.url.open(dialogInfo, submitHandler);
  };

  const duplicateDraftMessage = async (id: number) => {
    try {
      await duplicateDraftNotification(id);
      GetDraftMessagesSilentAction(dispatch);
    } catch (error) {
      return error;
    }
  };

  const deleteDraftMessage = async (id: number) => {
    try {
      await deleteDraftNotification(id);
      GetScheduledMessagesSilentAction(dispatch);
    } catch (error) {
      return error;
    }
  };

  const onPreviewMessageConfirmation = (url: string, title: string) => {
    const dialogInfo: UrlDialogInfo = {
      url,
      title,
      size: { height: DialogDimension.Small, width: DialogDimension.Small },
      fallbackUrl: url,
    };

    // now open the dialog
    dialog.url.open(dialogInfo);
  };

  const checkPreviewMessage = async (id: number) => {
    const payload = {
      draftNotificationId: id,
      teamsTeamId,
      teamsChannelId,
    };
    sendPreview(payload)
      .then(() => {
        onPreviewMessageConfirmation(previewConfirmationUrl(), t('previewMessageTitle'));
        return true;
      })
      .catch((error) => {
        return error;
      });
  };

  return (
    <Table {...keyboardNavAttr} role='grid' aria-label='Scheduled messages table with grid keyboard navigation'>
      <TableHeader>
        <TableRow>
          <TableHeaderCell key='title' style={{ width: '55%' }}>
            <Body1Strong>{t('TitleText')}</Body1Strong>
          </TableHeaderCell>
          <TableHeaderCell key='schedule'>
            <Body1Strong>{t('Schedule')}</Body1Strong>
          </TableHeaderCell>
          <TableHeaderCell key='actions' style={{ width: '50px' }}>
            <Body1Strong>{t('actions')}</Body1Strong>
          </TableHeaderCell>
        </TableRow>
      </TableHeader>
      <TableBody>
        {scheduledMessages.scheduledMessages?.map((item: any) => (
          // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
          <TableRow key={`${item.id}key`}>
            <TableCell tabIndex={0} role='gridcell'>
              <TableCellLayout
                truncate
                media={<Chat20Regular />}
                style={{ cursor: 'pointer' }}
                onClick={() => {
                  onOpenTaskModule(editUrl(item.id), t('EditMessage'));
                }}
              >
                <Body1Strong style={{ whiteSpace: 'nowrap' }}>{item.title}</Body1Strong>
              </TableCellLayout>
            </TableCell>
            <TableCell tabIndex={0} role='gridcell'>
              <TableCellLayout truncate>{formatDate(item.scheduledDate)}</TableCellLayout>
            </TableCell>
            <TableCell role='gridcell' style={{ width: '50px' }}>
              <TableCellLayout style={{ float: 'right' }}>
                <Menu>
                  <MenuTrigger disableButtonEnhancement>
                    <Button aria-label='Actions menu' icon={<MoreHorizontal24Filled />} />
                  </MenuTrigger>
                  <MenuPopover>
                    <MenuList>
                      <MenuItem
                        icon={<SendRegular />}
                        key={'sendConfirmationKey'}
                        onClick={() => {
                          onOpenTaskModule(sendUrl(item.id), t('SendConfirmation'));
                        }}
                      >
                        {t('SendNow')}
                      </MenuItem>
                      {
                        <MenuItem
                          key={'previewInThisChannelKey'}
                          icon={<OpenRegular />}
                        // eslint-disable-next-line @typescript-eslint/no-misused-promises, @typescript-eslint/promise-function-async
                          onClick={() => checkPreviewMessage(item.id)}
                        >
                          {t('PreviewInThisChannel')}
                        </MenuItem>
                      }
                      <MenuItem
                        icon={<EditRegular />}
                        key={'editMessageKey'}
                        onClick={() => {
                          onOpenTaskModule(editUrl(item.id), t('EditMessage'));
                        }}
                      >
                        {t('Edit')}
                      </MenuItem>
                      {
                        <MenuItem
                          key={'duplicateKey'}
                          icon={<DocumentCopyRegular />}
                          // eslint-disable-next-line @typescript-eslint/no-misused-promises, @typescript-eslint/promise-function-async
                          onClick={() => duplicateDraftMessage(item.id)}
                        >
                          {t('Duplicate')}
                        </MenuItem>
                      }
                      {
                        <MenuItem
                          key={'deleteKey'}
                          icon={<DeleteRegular />}
                        // eslint-disable-next-line @typescript-eslint/no-misused-promises, @typescript-eslint/promise-function-async
                          onClick={() => deleteDraftMessage(item.id)}
                        >
                          {t('Delete')}
                        </MenuItem>
                      }
                    </MenuList>
                  </MenuPopover>
                </Menu>
              </TableCellLayout>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
};
