import { sendWebsocketMsg } from "./request";
import { getConfigure, saveConfigure } from './configure'

export const get = () => {
    return getConfigure('Socks5ClientConfigure');
}
export const set = (data) => {
    return saveConfigure('Socks5ClientConfigure', JSON.stringify(data));
}
export const run = (data) => {
    return sendWebsocketMsg(`socks5/run`, data);
}
export const getPac = () => {
    return sendWebsocketMsg(`socks5/getpac`);
}
export const updatePac = (content) => {
    return sendWebsocketMsg(`socks5/updatepac`, content);
}
export const testProxy = (content) => {
    return sendWebsocketMsg(`socks5/test`, content);
}
