import { getSentNotifications, getDraftNotifications } from '../apis/messageListApi';

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
        notification.sendingDuration = formatNotificationSendingDuration(notification.sendingStartedDate, notification.sentDate);
        notification.sendingStartedDate = formatNotificationDate(notification.sendingStartedDate);
        notification.sentDate = formatNotificationDate(notification.sentDate);
    });
    dispatch({ type: 'FETCH_MESSAGES', payload: notificationList });
};

export const getDraftMessagesList = () => async (dispatch: any) => {
    const response = await getDraftNotifications();
    dispatch({ type: 'FETCH_DRAFTMESSAGES', payload: response.data });
};

const formatNotificationDate = (notificationDate: string) => {
    if (notificationDate) {
        notificationDate = (new Date(notificationDate)).toLocaleString(navigator.language, { year: 'numeric', month: 'numeric', day: 'numeric', hour: 'numeric', minute: 'numeric', hour12: true });
        notificationDate = notificationDate.replace(',', '\xa0\xa0');
    }
    return notificationDate;
}

const formatNotificationSendingDuration = (sendingStartedDate: string, sentDate: string) => {
    let sendingDuration = "";
    if (sendingStartedDate && sentDate) {
        let timeDifference = new Date(sentDate).getTime() - new Date(sendingStartedDate).getTime();
        sendingDuration = new Date(timeDifference).toISOString().substr(11, 8);
    }
    return sendingDuration;
}