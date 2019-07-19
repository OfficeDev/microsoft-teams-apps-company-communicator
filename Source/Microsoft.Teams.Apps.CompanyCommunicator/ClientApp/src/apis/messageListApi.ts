import axios from 'axios';
import { getBaseUrl } from '../configVariables';

let baseAxiosUrl = getBaseUrl() + '/api';

export const getSentNotifications = async (): Promise<any> => {
    // let url = baseAxiosUrl + "/sentnotifications";
    // return await axios.get(url);
    return await axios.get("https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/sentnotifications");
}

export const getDraftNotifications = async (): Promise<any> => {
    // let url = baseAxiosUrl + "/draftnotifications";
    // return await axios.get(url);
    return await axios.get("https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/draftnotifications");
}

export const getSentNotification = async (id: number): Promise<any> => {
    // let url = baseAxiosUrl + "/sentnotifications/" + id;
    // return await axios.get(url);
    let url = "https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/sentnotifications/" + id;
    return await axios.get(url);
}

export const getDraftNotification = async (id: number): Promise<any> => {
    // let url = baseAxiosUrl + "/draftnotifications/" + id;
    // return await axios.get(url);

    let url = "https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/draftnotifications/" + id;
    return await axios.get(url);
}

export const deleteDraftNotification = async (id: number): Promise<any> => {
    // let url = baseAxiosUrl + "/draftnotifications/" + id;
    // return await axios.get(url);

    let url = "https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/draftnotifications/" + id;
    return await axios.delete(url);
}

export const duplicateDraftNotification = async (id: number): Promise<any> => {
    // let url = baseAxiosUrl + "/draftnotifications/" + id;
    // return await axios.get(url);

    let url = "https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/draftnotifications/duplicates/" + id;
    return await axios.post(url);
}

export const sentDraftNotification = async (payload: {}): Promise<any> => {
    // let url = baseAxiosUrl + "/draftnotifications/" + id;
    // return await axios.get(url);

    return await axios.post("https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/sentnotifications",
        payload);
}

export const updateDraftNotification = async (payload: {}): Promise<any> => {
    // let url = baseAxiosUrl + "/draftnotifications";
    // return await axios.get(url);
    return await axios.put("https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/draftnotifications",
        payload);
}

export const creatDraftNotification = async (payload: {}): Promise<any> => {
    // let url = baseAxiosUrl + "/draftnotifications";
    // return await axios.get(url);
    return await axios.post("https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/draftnotifications",
        payload, {
            headers: {
                'Accept': '*/*',
                'Content-Type': 'application/json',
                'Cache-Control': 'no-cache',
            }
        });
}

export const getTeams = async (): Promise<any> => {
    // let url = baseAxiosUrl + "/getTeam/";
    return await axios.get("https://microsoftteamsappscompanycommunicator20190717051342.azurewebsites.net/api/teamsdata/channel");
}