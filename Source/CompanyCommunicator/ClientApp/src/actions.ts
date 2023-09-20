// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  getDeletedMessages,
  getDraftNotifications,
  getGroups,
  getSentNotifications,
  getTeams,
  searchGroups,
  verifyGroupAccess,
  getScheduledDraftNotifications,
} from './apis/messageListApi';
import { formatDate } from './i18n';
import {
  deletedMessages,
  draftMessages,
  groups,
  isDeletedMessagesFetchOn,
  isDraftMessagesFetchOn,
  isSentMessagesFetchOn,
  queryGroups,
  selectedMessage,
  sentMessages,
  teamsData,
  verifyGroup,
  isScheduledMessagesFetchOn,
  scheduledMessages,
} from './messagesSlice';
import { store } from './store';

interface Notification {
  createdDateTime: string;
  failed: number;
  id: string;
  isCompleted: boolean;
  sentDate: string;
  sendingStartedDate: string;
  sendingDuration: string;
  succeeded: number;
  throttled: number;
  title: string;
  totalMessageCount: number;
  createdBy: string;
  scheduledDate: string;
}

export const SelectedMessageAction = (dispatch: typeof store.dispatch, payload: any) => {
  dispatch(selectedMessage({ type: 'MESSAGE_SELECTED', payload }));
};

export const GetSentMessagesAction = (dispatch: typeof store.dispatch) => {
  SentMessageFetchStatusAction(dispatch, true);
  getSentNotifications()
    .then((response) => {
      const notificationList: Notification[] = response || [];
      notificationList.forEach((notification) => {
        notification.sendingStartedDate = formatDate(notification.sendingStartedDate);
        notification.sentDate = formatDate(notification.sentDate);
      });
      dispatch(sentMessages({ type: 'FETCH_MESSAGES', payload: notificationList || [] }));
    })
    .finally(() => {
      SentMessageFetchStatusAction(dispatch, false);
    });
};

export const GetSentMessagesSilentAction = (dispatch: typeof store.dispatch) => {
  void getSentNotifications().then((response) => {
    const notificationList: Notification[] = response || [];
    notificationList.forEach((notification) => {
      notification.sendingStartedDate = formatDate(notification.sendingStartedDate);
      notification.sentDate = formatDate(notification.sentDate);
    });
    dispatch(sentMessages({ type: 'FETCH_MESSAGES', payload: notificationList || [] }));
  });
};

export const GetDraftMessagesAction = (dispatch: typeof store.dispatch) => {
  DraftMessageFetchStatusAction(dispatch, true);
  getDraftNotifications()
    .then((response) => {
      dispatch(draftMessages({ type: 'FETCH_DRAFT_MESSAGES', payload: response || [] }));
    })
    .finally(() => {
      DraftMessageFetchStatusAction(dispatch, false);
    });
};

export const GetDraftMessagesSilentAction = (dispatch: typeof store.dispatch) => {
  void getDraftNotifications().then((response) => {
    dispatch(draftMessages({ type: 'FETCH_DRAFT_MESSAGES', payload: response || [] }));
  });
};

export const GetScheduledMessagesAction = (dispatch: typeof store.dispatch) => {
  ScheduledMessageFetchStatusAction(dispatch, true);
  getScheduledDraftNotifications()
    .then((response) => {
      dispatch(scheduledMessages({ type: 'FETCH_SCHEDULED_MESSAGES', payload: response || [] }));
    })
    .finally(() => {
      ScheduledMessageFetchStatusAction(dispatch, false);
    });
};

export const GetScheduledMessagesSilentAction = (dispatch: typeof store.dispatch) => {
  void getScheduledDraftNotifications().then((response) => {
    dispatch(scheduledMessages({ type: 'FETCH_SCHEDULED_MESSAGES', payload: response || [] }));
  });
};

export const GetDeletedMessagesAction = (dispatch: typeof store.dispatch) => {
  DeletedMessageFetchStatusAction(dispatch, true);
  getDeletedMessages()
    .then((response) => {
      dispatch(deletedMessages({ type: 'FETCH_DELETED_MESSAGES', payload: response || [] }));
    })
    .finally(() => {
      DeletedMessageFetchStatusAction(dispatch, false);
    });
};

export const GetDeletedMessagesSilentAction = (dispatch: typeof store.dispatch) => {
  void getDeletedMessages().then((response) => {
    dispatch(deletedMessages({ type: 'FETCH_DELETED_MESSAGES', payload: response || [] }));
  });
};

export const GetTeamsDataAction = (dispatch: typeof store.dispatch) => {
  void getTeams().then((response) => {
    dispatch(teamsData({ type: 'GET_TEAMS_DATA', payload: response || [] }));
  });
};

export const GetGroupsAction = (dispatch: typeof store.dispatch, payload: { id: number }) => {
  void getGroups(payload.id).then((response) => {
    dispatch(groups({ type: 'GET_GROUPS', payload: response || [] }));
  });
};

export const SearchGroupsAction = (dispatch: typeof store.dispatch, payload: { query: string }) => {
  void searchGroups(payload.query)
    .then((response) => {
      let output = [];
      try {
        output = JSON.parse(response);
      } catch {
        output = [];
      }
      dispatch(queryGroups({ type: 'SEARCH_GROUPS', payload: output }));
    })
    .catch(() => {
      dispatch(queryGroups({ type: 'SEARCH_GROUPS', payload: [] }));
    });
};

export const VerifyGroupAccessAction = (dispatch: typeof store.dispatch) => {
  verifyGroupAccess()
    .then((response) => {
      dispatch(verifyGroup({ type: 'VERIFY_GROUP_ACCESS', payload: true }));
    })
    .catch((error) => {
      const errorStatus = error.response.status;
      if (errorStatus === 403) {
        dispatch(verifyGroup({ type: 'VERIFY_GROUP_ACCESS', payload: false }));
      } else {
        throw error;
      }
    });
};

export const DraftMessageFetchStatusAction = (dispatch: typeof store.dispatch, payload: boolean) => {
  dispatch(isDraftMessagesFetchOn({ type: 'DRAFT_MESSAGES_FETCH_STATUS', payload }));
};

export const DeletedMessageFetchStatusAction = (dispatch: typeof store.dispatch, payload: boolean) => {
  dispatch(isDeletedMessagesFetchOn({ type: 'DELETED_MESSAGES_FETCH_STATUS', payload }));
};

export const SentMessageFetchStatusAction = (dispatch: typeof store.dispatch, payload: boolean) => {
  dispatch(isSentMessagesFetchOn({ type: 'SENT_MESSAGES_FETCH_STATUS', payload }));
};

export const ScheduledMessageFetchStatusAction = (dispatch: typeof store.dispatch, payload: boolean) => {
  dispatch(isScheduledMessagesFetchOn({ type: 'SCHEDULED_MESSAGES_FETCH_STATUS', payload }));
};
