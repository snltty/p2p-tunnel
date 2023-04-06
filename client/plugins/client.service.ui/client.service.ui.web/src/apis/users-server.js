import { sendWebsocketMsg } from "./request";

export const getPage = (arg) => {
    return sendWebsocketMsg(`serverusers/list`, arg);
}

export const add = (row) => {
    return sendWebsocketMsg(`serverusers/add`, row);
}
export const remove = (id) => {
    return sendWebsocketMsg(`serverusers/remove`, id);
}
export const info = () => {
    return sendWebsocketMsg(`serverusers/info`);
}

export const setPassword = (password) => {
    return sendWebsocketMsg(`serverusers/password`, password);
}