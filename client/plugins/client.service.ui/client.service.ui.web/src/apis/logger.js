import { sendWebsocketMsg } from "./request";

export const getLoggers = (page) => {
    return sendWebsocketMsg(`logger/list`, page);
}
export const clearLoggers = () => {
    return sendWebsocketMsg(`logger/clear`);
}

export const getConfig = () => {
    return sendWebsocketMsg(`configure/configure`, 'LoggerClientConfigure');
}
export const updateConfig = (content) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: 'LoggerClientConfigure', Content: content });
}