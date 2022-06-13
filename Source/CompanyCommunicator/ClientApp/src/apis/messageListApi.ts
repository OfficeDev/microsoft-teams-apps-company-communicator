// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import axios from './axiosJWTDecorator';
import { getBaseUrl } from '../configVariables';

let baseAxiosUrl = getBaseUrl() + '/api';

export const getSentNotifications = async (): Promise<any> => {
    let url = baseAxiosUrl + "/sentnotifications";
    return await axios.get(url);
}

export const getChannelSentNotifications = async (channelId: string): Promise<any> => {
    let url = baseAxiosUrl + "/sentnotifications/channel/" + channelId;
    return await axios.get(url);
}

export const getScheduledNotifications = async (): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/scheduled";
    return await axios.get(url);
}

export const getChannelScheduledNotifications = async (channelId: string): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/scheduled/channel/" + channelId;
    return await axios.get(url);
}

export const getDraftNotifications = async (): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications";
    return await axios.get(url);
}

export const getChannelDraftNotifications = async (channelId: string): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/channel/" + channelId;
    return await axios.get(url);
}

export const verifyGroupAccess = async (): Promise<any> => {
    let url = baseAxiosUrl + "/groupdata/verifyaccess";
    return await axios.get(url, false);
}

export const getGroups = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/groupdata/" + id;
    return await axios.get(url);
}

export const searchGroups = async (query: string): Promise<any> => {
    let url = baseAxiosUrl + "/groupdata/search/" + query;
    return await axios.get(url);
}

export const exportNotification = async(payload: {}): Promise<any> => {
    let url = baseAxiosUrl + "/exportnotification/export";
    return await axios.put(url, payload);
}

export const getSentNotification = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/sentnotifications/" + id;
    return await axios.get(url);
}

export const getDraftNotification = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/" + id;
    return await axios.get(url);
}

export const deleteDraftNotification = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/" + id;
    return await axios.delete(url);
}

export const duplicateDraftNotification = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/duplicates/" + id;
    return await axios.post(url);
}

export const sendDraftNotification = async (payload: {}): Promise<any> => {
    let url = baseAxiosUrl + "/sentnotifications";
    return await axios.post(url, payload);
}

export const updateDraftNotification = async (payload: {}): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications";
    return await axios.put(url, payload);
}

export const createDraftNotification = async (payload: {}): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications";
    return await axios.post(url, payload);
}

//creates an association between a group and a channel (for targeting)
export const createGroupAssociation = async (payload: {}): Promise<any> => {
    let url = baseAxiosUrl + "/groupassociations";
    return await axios.post(url, payload);
}

//deletes an association between a group and a channel (for targeting)
export const deleteGroupAssociation = async (key: string): Promise<any> => {
    let url = baseAxiosUrl + "/groupassociations/" + key;
    return await axios.delete(url);
}

//gets all groups associated to a specific channel (for targeting)
export const getGroupAssociations = async (channelId: string): Promise<any> => {
    let url = baseAxiosUrl + "/groupassociations/" + channelId;
    return await axios.get(url);
}

export const getChannelConfig = async (channelId: string): Promise<any> => {
    let url = baseAxiosUrl + "/channels/" + channelId;
    return await axios.get(url);
}

export const updateChannelConfig = async (payload: {}): Promise<any> => {
    let url = baseAxiosUrl + "/channels";
    return await axios.put(url, payload);
}

export const getTeams = async (): Promise<any> => {
    let url = baseAxiosUrl + "/teamdata";
    return await axios.get(url);
}

export const cancelSentNotification = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/sentnotifications/cancel/" + id;
    return await axios.post(url);
}

export const getAppSettings = async (): Promise<any> => {
    let url = baseAxiosUrl + "/settings";
    return await axios.get(url);
}

export const getConsentSummaries = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/consentSummaries/" + id;
    return await axios.get(url);
}

export const sendPreview = async (payload: {}): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications/previews";
    return await axios.post(url, payload);
}

export const getAuthenticationConsentMetadata = async (windowLocationOriginDomain: string, login_hint: string): Promise<any> => {
    let url = `${baseAxiosUrl}/authenticationMetadata/consentUrl?windowLocationOriginDomain=${windowLocationOriginDomain}&loginhint=${login_hint}`;
    return await axios.get(url, undefined, false);
}
