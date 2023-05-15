import { sendWebsocketMsg } from "./request";

export const get = () => {
    return sendWebsocketMsg(`serverproxy/get`);
}
export const add = (data) => {
    return sendWebsocketMsg(`serverproxy/add`, data);
}
export const remove = (id) => {
    return sendWebsocketMsg(`serverproxy/remove`, id);
}