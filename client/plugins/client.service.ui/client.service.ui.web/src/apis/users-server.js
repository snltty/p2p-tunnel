import { sendWebsocketMsg } from "./request";

export const getPage = (p = 1, ps = 10) => {
    return sendWebsocketMsg(`serverusers/list`, {
        Page: p,
        PageSize: ps
    });
}