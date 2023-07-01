import { sendWebsocketMsg } from "./request";
import { getConfigure, saveConfigure } from './configure'

export const getLoggers = (page) => {
    return sendWebsocketMsg(`logger/list`, page);
}
export const clearLoggers = () => {
    return sendWebsocketMsg(`logger/clear`);
}

export const getConfig = () => {
    return getConfigure('LoggerClientConfigure');
}
export const updateConfig = (content) => {
    return saveConfigure('LoggerClientConfigure', content);
}