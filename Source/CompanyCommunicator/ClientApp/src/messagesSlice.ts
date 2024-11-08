// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { HostClientType } from '@microsoft/teams-js';
import { createSlice } from '@reduxjs/toolkit';

export interface MessagesState {
  draftMessages: { action: string; payload: [] };
  sentMessages: { action: string; payload: [] };
  deletedMessages: { action: string; payload: [] };
  selectedMessage: { action: string; payload: any };
  teamsData: { action: string; payload: any[] };
  groups: { action: string; payload: any[] };
  queryGroups: { action: string; payload: any[] };
  verifyGroup: { action: string; payload: boolean };
  isDraftMessagesFetchOn: { action: string; payload: boolean };
  isSentMessagesFetchOn: { action: string; payload: boolean };
  isDeletedMessagesFetchOn: { action: string; payload: boolean };
  hostClientType: { action: string; payload?: HostClientType };
  scheduledMessages: { action: string; payload: [] };
  isScheduledMessagesFetchOn: { action: string; payload: boolean };
}

const initialState: MessagesState = {
  draftMessages: { action: 'FETCH_DRAFT_MESSAGES', payload: [] },
  sentMessages: { action: 'FETCH_MESSAGES', payload: [] },
  deletedMessages: { action: 'FETCH_DELETED_MESSAGES', payload: [] },
  selectedMessage: { action: 'MESSAGE_SELECTED', payload: [] },
  teamsData: { action: 'GET_TEAMS_DATA', payload: [] },
  groups: { action: 'GET_GROUPS', payload: [] },
  queryGroups: { action: 'SEARCH_GROUPS', payload: [] },
  verifyGroup: { action: 'VERIFY_GROUP_ACCESS', payload: false },
  isDraftMessagesFetchOn: { action: 'DRAFT_MESSAGES_FETCH_STATUS', payload: false },
  isSentMessagesFetchOn: { action: 'SENT_MESSAGES_FETCH_STATUS', payload: false },
  isDeletedMessagesFetchOn: { action: 'DELETED_MESSAGES_FETCH_STATUS', payload: false },
  hostClientType: { action: 'HOST_CLIENT_TYPE' },
  scheduledMessages: { action: 'FETCH_SCHEDULED_MESSAGES', payload: [] },
  isScheduledMessagesFetchOn: { action: 'SCHEDULED_MESSAGES_FETCH_STATUS', payload: false },
};

export const messagesSlice = createSlice({
  name: 'messagesSlice',
  initialState,
  reducers: {
    draftMessages: (state, action) => {
      state.draftMessages = action.payload;
    },
    sentMessages: (state, action) => {
      state.sentMessages = action.payload;
    },
    deletedMessages: (state, action) => {
      state.deletedMessages = action.payload;
    },
    selectedMessage: (state, action) => {
      state.selectedMessage = action.payload;
    },
    teamsData: (state, action) => {
      state.teamsData = action.payload;
    },
    groups: (state, action) => {
      state.groups = action.payload;
    },
    queryGroups: (state, action) => {
      state.queryGroups = action.payload;
    },
    verifyGroup: (state, action) => {
      state.verifyGroup = action.payload;
    },
    isDraftMessagesFetchOn: (state, action) => {
      state.isDraftMessagesFetchOn = action.payload;
    },
    isSentMessagesFetchOn: (state, action) => {
      state.isSentMessagesFetchOn = action.payload;
    },
    isDeletedMessagesFetchOn: (state, action) => {
      state.isDeletedMessagesFetchOn = action.payload;
    },
    hostClientType: (state, action) => {
      state.hostClientType = action.payload;
    },
    scheduledMessages: (state, action) => {
      state.scheduledMessages = action.payload;
    },
    isScheduledMessagesFetchOn: (state, action) => {
      state.isScheduledMessagesFetchOn = action.payload;
    },
  },
});

export const {
  draftMessages,
  sentMessages,
  deletedMessages,
  selectedMessage,
  teamsData,
  groups,
  queryGroups,
  verifyGroup,
  isDraftMessagesFetchOn,
  isSentMessagesFetchOn,
  isDeletedMessagesFetchOn,
  hostClientType,
  scheduledMessages,
  isScheduledMessagesFetchOn,
} = messagesSlice.actions;

export default messagesSlice.reducer;
