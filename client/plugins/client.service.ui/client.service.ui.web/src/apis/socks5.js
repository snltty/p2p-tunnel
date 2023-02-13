import { sendWebsocketMsg } from "./request";

export const get = () => {
    return sendWebsocketMsg(`socks5/get`);
}
export const set = (data) => {
    return sendWebsocketMsg(`socks5/set`, data);
}
export const getPac = () => {
    return sendWebsocketMsg(`socks5/getpac`);
}
export const updatePac = (content) => {
    return sendWebsocketMsg(`socks5/updatepac`, content);
}