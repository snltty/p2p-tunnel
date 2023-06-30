import { sendWebsocketMsg } from "./request";

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

export const getOnlines = (id) => {
    return sendWebsocketMsg(`vea/online`, id);
}

export const onlines = (id) => {
    return sendWebsocketMsg(`vea/onlines`, id);
}

export const test = (host, port) => {
    return sendWebsocketMsg(`vea/test`, { host, port });
}