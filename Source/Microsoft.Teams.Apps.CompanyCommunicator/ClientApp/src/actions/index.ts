import messageList from '../apis/messageList';

export const selectMessage = (message: any) => {
    return {
        type: 'MESSAGE_SELECTED',
        payload: message
    };
};

export const getMessagesList = () => async (dispatch: any) => {
    const response = await messageList.get('/sentnotifications');
    dispatch({ type: 'FETCH_MESSAGES', payload: response.data });
};

export const getDraftMessagesList = () => async (dispatch: any) => {
    const response = await messageList.get('/draftnotifications');
    dispatch({ type: 'FETCH_DRAFTMESSAGES', payload: response.data });
};