import { sendWebsocketMsg } from "./request";

export const sendSignInMsg = () => {
    return sendWebsocketMsg(`signin/start`);
}
export const sendExit = () => {
    return sendWebsocketMsg(`signin/exit`);
}
export const sendPing = (ips) => {
    return sendWebsocketMsg(`signin/ping`, ips);
}


export const getSignInInfo = () => {
    return sendWebsocketMsg(`signin/info`);
}

export const updateConfig = (config) => {
    return sendWebsocketMsg(`signin/config`, config);
}