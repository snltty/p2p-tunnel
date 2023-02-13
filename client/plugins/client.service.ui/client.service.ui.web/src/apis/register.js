import { sendWebsocketMsg } from "./request";

export const sendRegisterMsg = () => {
    return sendWebsocketMsg(`register/start`);
}
export const sendExit = () => {
    return sendWebsocketMsg(`register/exit`);
}
export const sendPing = (ips) => {
    return sendWebsocketMsg(`register/ping`, ips);
}


export const getRegisterInfo = () => {
    return sendWebsocketMsg(`register/info`);
}

export const updateConfig = (config) => {
    return sendWebsocketMsg(`register/config`, config);
}