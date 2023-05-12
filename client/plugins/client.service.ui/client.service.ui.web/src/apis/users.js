import { sendWebsocketMsg } from "./request";

export const getPage = (arg) => {
    return sendWebsocketMsg(`users/list`, arg);
}
export const update = (row) => {
    return sendWebsocketMsg(`users/update`, row);
}