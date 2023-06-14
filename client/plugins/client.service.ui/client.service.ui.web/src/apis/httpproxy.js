import { sendWebsocketMsg } from "./request";

export const update = () => {
    return sendWebsocketMsg(`httpproxy/update`);
}
export const getPac = () => {
    return sendWebsocketMsg(`httpproxy/GetPac`);
}
export const setPac = (content) => {
    return sendWebsocketMsg(`httpproxy/SetPac`, content);
}
export const testProxy = () => {
    return sendWebsocketMsg(`httpproxy/test`);
}