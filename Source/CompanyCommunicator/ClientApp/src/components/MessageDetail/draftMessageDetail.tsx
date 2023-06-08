// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from "react";
import { useTranslation } from "react-i18next";
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
} from "@fluentui/react-components";
import {
  DeleteRegular,
  DocumentCopyRegular,
  Chat20Regular,
  EditRegular,
  MoreHorizontal24Filled,
  OpenRegular,
  SendRegular,
} from "@fluentui/react-icons";
import * as microsoftTeams from "@microsoft/teams-js";
import { GetDraftMessagesSilentAction, GetSentMessagesSilentAction } from "../../actions";
import { deleteDraftNotification, duplicateDraftNotification, sendPreview } from "../../apis/messageListApi";
import { getBaseUrl } from "../../configVariables";
import { ROUTE_PARTS, ROUTE_QUERY_PARAMS } from "../../routes";
import { useAppDispatch } from "../../store";

export const DraftMessageDetail = (draftMessages: any) => {
  const { t } = useTranslation();
  const keyboardNavAttr = useArrowNavigationGroup({ axis: "grid" });
  const [teamsTeamId, setTeamsTeamId] = React.useState("");
  const [teamsChannelId, setTeamsChannelId] = React.useState("");
  const dispatch = useAppDispatch();
  const sendUrl = (id: string) =>
    getBaseUrl() + `/${ROUTE_PARTS.SEND_CONFIRMATION}/${id}?${ROUTE_QUERY_PARAMS.LOCALE}={locale}`;
  const editUrl = (id: string) =>
    getBaseUrl() + `/${ROUTE_PARTS.NEW_MESSAGE}/${id}?${ROUTE_QUERY_PARAMS.LOCALE}={locale}`;

  React.useEffect(() => {
    microsoftTeams.getContext((context: microsoftTeams.Context) => {
      setTeamsTeamId(context.teamId || "");
      setTeamsChannelId(context.channelId || "");
    });
  }, []);

  const submitHandler = (err: any, result: any) => {
    GetDraftMessagesSilentAction(dispatch);
    GetSentMessagesSilentAction(dispatch);
  };

  const onOpenTaskModule = (event: any, url: string, title: string) => {
    let taskInfo: microsoftTeams.TaskInfo = {
      url: url,
      title: title,
      height: microsoftTeams.TaskModuleDimension.Large,
      width: microsoftTeams.TaskModuleDimension.Large,
      fallbackUrl: url,
    };

    microsoftTeams.tasks.startTask(taskInfo, submitHandler);
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
      GetDraftMessagesSilentAction(dispatch);
    } catch (error) {
      return error;
    }
  };

  const checkPreviewMessage = async (id: number) => {
    let payload = {
      draftNotificationId: id,
      teamsTeamId: teamsTeamId,
      teamsChannelId: teamsChannelId,
    };
    sendPreview(payload)
      .then((response) => {
        return response.status;
      })
      .catch((error) => {
        return error;
      });
  };

  return (
    <Table {...keyboardNavAttr} role='grid' aria-label='Draft messages table with grid keyboard navigation'>
      <TableHeader>
        <TableRow>
          <TableHeaderCell key='title'>
            <b>{t('TitleText')}</b>
          </TableHeaderCell>
          <TableHeaderCell key='actions' style={{ width: '50px' }}>
            <b>Actions</b>
          </TableHeaderCell>
        </TableRow>
      </TableHeader>
      <TableBody>
        {draftMessages!.draftMessages!.map((item: any) => (
          <TableRow key={item.id + 'key'}>
            <TableCell tabIndex={0} role='gridcell'>
              <TableCellLayout
                truncate
                media={<Chat20Regular />}
                style={{ cursor: 'pointer' }}
                onClick={() => onOpenTaskModule(null, editUrl(item.id), t('EditMessage'))}
              >
                {item.title}
              </TableCellLayout>
            </TableCell>
            <TableCell role='gridcell' style={{ width: '50px' }}>
              <TableCellLayout>
                <Menu>
                  <MenuTrigger disableButtonEnhancement>
                    <Button aria-label='Actions menu' icon={<MoreHorizontal24Filled />} />
                  </MenuTrigger>
                  <MenuPopover>
                    <MenuList>
                      <MenuItem
                        icon={<SendRegular />}
                        key={'sendConfirmationKey'}
                        onClick={() => onOpenTaskModule(null, sendUrl(item.id), t('SendConfirmation'))}
                      >
                        {t('Send')}
                      </MenuItem>
                      <MenuItem key={'previewInThisChannelKey'} icon={<OpenRegular />} onClick={() => checkPreviewMessage(item.id)}>
                        {t('PreviewInThisChannel')}
                      </MenuItem>
                      <MenuItem
                        icon={<EditRegular />}
                        key={'editMessageKey'}
                        onClick={() => onOpenTaskModule(null, editUrl(item.id), t('EditMessage'))}
                      >
                        {t('Edit')}
                      </MenuItem>
                      <MenuItem key={'duplicateKey'} icon={<DocumentCopyRegular />} onClick={() => duplicateDraftMessage(item.id)}>
                        {t('Duplicate')}
                      </MenuItem>
                      <MenuItem key={'deleteKey'} icon={<DeleteRegular />} onClick={() => deleteDraftMessage(item.id)}>
                        {t('Delete')}
                      </MenuItem>
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
