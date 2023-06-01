import { sendWebsocketMsg } from "./request";

export const get = () => {
    return sendWebsocketMsg(`proxy/get`);
}
export const setHeaders = (data) => {
    return sendWebsocketMsg(`proxy/setHeaders`, data);
}
export const add = (data) => {
    return sendWebsocketMsg(`proxy/add`, data);
}
export const remove = (id) => {
    return sendWebsocketMsg(`proxy/remove`, id);
}