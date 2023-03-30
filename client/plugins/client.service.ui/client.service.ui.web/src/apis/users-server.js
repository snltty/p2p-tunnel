import { sendWebsocketMsg } from "./request";

export const getPage = (p = 1, ps = 10) => {
    return sendWebsocketMsg(`serverusers/list`, {
        Page: p,
        PageSize: ps
    });
}

export const add = (row) => {
    return sendWebsocketMsg(`serverusers/add`, row);
}
export const remove = (id) => {
    return sendWebsocketMsg(`serverusers/remove`, id);
}
export const setPassword = (password) => {
    return sendWebsocketMsg(`serverusers/password`, password);
}