// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export enum ROUTE_PARTS {
  CONFIG_TAB = 'configtab',
  MESSAGES = 'messages',
  NEW_MESSAGE = 'newmessage',
  DELETE_MESSAGES = 'deletemessages',
  DELETE_MESSAGES_CONFIRM = 'deleteconfirmation',
  VIEW_STATUS = 'viewstatus',
  SEND_CONFIRMATION = 'sendconfirmation',
  ERROR_PAGE = 'errorpage',
  SIGN_IN = 'signin',
  SIGN_IN_SIMPLE_START = 'signin-simple-start',
  SIGN_IN_SIMPLE_END = 'signin-simple-end',
  PREVIEW_MESSAGE_CONFIRMATION = 'previewMessageConfirmation',
}

export enum ROUTE_PARAMS {
  ID = 'id',
  DELETION_TYPE = 'deletionType',
  DELETION_FROM_DATE = 'deletionFromDate',
  DELETION_TO_DATE = 'deletionToDate',
}

export enum ROUTE_QUERY_PARAMS {
  LOCALE = 'locale',
}
