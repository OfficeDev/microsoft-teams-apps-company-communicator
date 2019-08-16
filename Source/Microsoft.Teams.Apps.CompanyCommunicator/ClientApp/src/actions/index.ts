import { getSentNotifications, getDraftNotifications } from '../apis/messageListApi';

type Notification = {
    createdDateTime: string,
    failed: number,
    id: string,
    isCompleted: boolean,
    sentDate: string,
    succeeded: number,
    throttled: number,
    title: string,
}

export const selectMessage = (message: any) => {
    return {
        type: 'MESSAGE_SELECTED',
        payload: message
    };
};

export const getMessagesList = () => async (dispatch: any) => {
    const response = await getSentNotifications();
    const notificationList: Notification[] = response.data;
    notificationList.forEach(notification => {
        notification.sentDate = formatNotificationDate(notification.sentDate);
    });
    dispatch({ type: 'FETCH_MESSAGES', payload: notificationList });
};

export const getDraftMessagesList = () => async (dispatch: any) => {
    const response = await getDraftNotifications();
    dispatch({ type: 'FETCH_DRAFTMESSAGES', payload: response.data });
};

const formatNotificationDate = (notification: string) => {
    if (notification) {
        notification = (new Date(notification)).toLocaleString(navigator.language, { year: 'numeric', month: 'numeric', day: 'numeric', hour: 'numeric', minute: 'numeric', hour12: true });
        notification = notification.replace(',', '');
    }
    return notification;
}