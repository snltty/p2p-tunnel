import { sendWebsocketMsg } from "./request";

export const get = () => {
    return sendWebsocketMsg(`configure/configure`, 'Socks5ClientConfigure');
}
export const set = (data) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: 'LoggerClientConfigure', Content: JSON.stringify(data) });
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
