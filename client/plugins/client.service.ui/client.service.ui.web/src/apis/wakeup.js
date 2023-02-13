import { sendWebsocketMsg } from "./request";

export const get = () => {
    return sendWebsocketMsg(`wakeup/get`);
}
export const getList = () => {
    return sendWebsocketMsg(`wakeup/list`);
}

export const wakeup = (data) => {
    return sendWebsocketMsg(`wakeup/wakeup`, data);
}
export const update = () => {
    return sendWebsocketMsg(`wakeup/update`);
}

export const getConfig = () => {
    return sendWebsocketMsg(`configure/configure`, 'WakeUpClientConfigure');
}
export const updateConfig = (content) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: 'WakeUpClientConfigure', Content: content });
}