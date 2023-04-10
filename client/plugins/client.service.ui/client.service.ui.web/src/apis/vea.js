import { sendWebsocketMsg } from "./request";

export const getConfig = () => {
    return sendWebsocketMsg(`vea/get`);
}
export const setConfig = (data) => {
    return sendWebsocketMsg(`vea/set`, data);
}
export const runVea = (data) => {
    return sendWebsocketMsg(`vea/run`, data);
}
export const getList = () => {
    return sendWebsocketMsg(`vea/list`);
}
export const reset = (id) => {
    return sendWebsocketMsg(`vea/reset`, id);
}
export const update = () => {
    return sendWebsocketMsg(`vea/update`);
}