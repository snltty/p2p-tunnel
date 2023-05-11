import { sendWebsocketMsg } from "./request";

export const get = () => {
    return sendWebsocketMsg(`proxy/get`);
}
export const add = (data) => {
    return sendWebsocketMsg(`proxy/add`, data);
}
export const remove = (id) => {
    return sendWebsocketMsg(`proxy/remove`, id);
}