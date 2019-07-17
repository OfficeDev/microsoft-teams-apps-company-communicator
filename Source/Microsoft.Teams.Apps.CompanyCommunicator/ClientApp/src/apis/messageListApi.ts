import axios from 'axios';
import { getBaseUrl } from '../configVariables';

let baseAxiosUrl = getBaseUrl() + '/api';

export const getSentNotifications = async (): Promise<any> => {
    let url = baseAxiosUrl + "/sentnotifications";
    return await axios.get(url);
}

export const getDraftNotifications = async (): Promise<any> => {
    let url = baseAxiosUrl + "/draftnotifications";
    return await axios.get(url);
}

export const getSentNotification = async (id: number): Promise<any> => {
    let url = baseAxiosUrl + "/sentnotifications/" + id;
    return await axios.get(url);
}