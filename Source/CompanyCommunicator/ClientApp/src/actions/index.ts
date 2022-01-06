// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { formatDate } from '../i18n';
import {
    getAppSettings, getSentNotifications, getChannelSentNotifications, getDraftNotifications,
    getChannelDraftNotifications, getChannelScheduledNotifications, getScheduledNotifications
} from '../apis/messageListApi';
import * as microsoftTeams from "@microsoft/teams-js";

type Notification = {
    createdDateTime: string,
    failed: number,
    id: string,
    isCompleted: boolean,
    sentDate: string,
    sendingStartedDate: string,
    sendingDuration: string,
    succeeded: number,
    throttled: number,
    title: string,
    totalMessageCount: number,
    scheduledDate: string,
}

//async function to get configuration values from Azure
async function getTargetingEnabled() {
    var response = await getAppSettings();
    var targetingEnabled = false;

    if (response.data) {
        targetingEnabled = (response.data.targetingEnabled === 'true'); //get the targetingenabled value
    }

    return targetingEnabled;
}

//function to return the Teams Channel ID
function getTeamsChannelId(): any {
    return new Promise((resolve) => {
        microsoftTeams.getContext(context => {
            resolve(context.channelId);
        });
    });
}

//select a message
export const selectMessage = (message: any) => {
    return {
        type: 'MESSAGE_SELECTED',
        payload: message
    };
};

//get the list of sent messages
export const getMessagesList = () => async (dispatch: any) => {
    var response; //response
    //get control values to decide about targeting behavior
    var targetingEnabled = await getTargetingEnabled();
    var teamsChannelId = await getTeamsChannelId();

    //in case of targeting is enabled, returns list filtered by channel id
    if (targetingEnabled) {
        response = await getChannelSentNotifications(teamsChannelId); //need to change
    }
    else {
        response = await getSentNotifications();
    }

    //coverts the response into a list
    const notificationList: Notification[] = response.data;

    //format dates in the list
    notificationList.forEach(notification => {
        notification.sendingStartedDate = formatDate(notification.sendingStartedDate);
        notification.sentDate = formatDate(notification.sentDate);
    });

    //dispatch the payload
    dispatch({ type: 'FETCH_MESSAGES', payload: notificationList });
};

//get the list of draft messages based on targeting. if disabled, all drafts are returned. if enabled list is filtered by channel id.
export const getDraftMessagesList = () => async (dispatch: any) => {
    var response; //response

    //get control values to decide about targeting behavior
    var targetingEnabled = await getTargetingEnabled();
    var teamsChannelId = await getTeamsChannelId();

    //in case of targeting is enabled, returns list filtered by channel id
    if (targetingEnabled) {
        response = await getChannelDraftNotifications(teamsChannelId);
    }
    else { //targeting disabled, all draft messages are returned
        response = await getDraftNotifications();
    }

    //dispatch the payload
    dispatch({ type: 'FETCH_DRAFTMESSAGES', payload: response.data });
};

//gets the list of scheduled messages based on targeting. if disabled, all scheduled drafts are returned. if enabled, list is filtered by channel id.
export const getScheduledMessagesList = () => async (dispatch: any) => {
    var response; //response

    //get control values to decide about targeting behavior
    var targetingEnabled = await getTargetingEnabled();
    var teamsChannelId = await getTeamsChannelId();

    //in case of targeting is enabled, returns list filtered by channel id
    if (targetingEnabled) {
        response = await getChannelScheduledNotifications(teamsChannelId);
    }
    else {
        response = await getScheduledNotifications();
    }

    //coverts the response into a list
    const notificationList: Notification[] = response.data;

    //format dates in the list
    notificationList.forEach(notification => {
        notification.scheduledDate = formatDate(notification.scheduledDate);
    });

    //dispatch the list
    dispatch({ type: 'FETCH_SCHEDULEDMESSAGES', payload: notificationList });
};