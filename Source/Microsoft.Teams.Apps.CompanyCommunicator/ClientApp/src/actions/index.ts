import { getMessageList, getDraftMessage } from '../apis/messageListApi';

export const selectMessage = (message: any) => {
    return {
        type: 'MESSAGE_SELECTED',
        payload: message
    };
};

export const getMessagesList = () => async (dispatch: any) => {
    const response = await getMessageList();
    dispatch({ type: 'FETCH_MESSAGES', payload: response.data });
};

export const getDraftMessagesList = () => async (dispatch: any) => {
    const response = await getDraftMessage();
    dispatch({ type: 'FETCH_DRAFTMESSAGES', payload: response.data });
};