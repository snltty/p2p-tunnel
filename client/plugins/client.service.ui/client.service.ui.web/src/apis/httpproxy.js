import { sendWebsocketMsg } from "./request";

export const update = () => {
    return sendWebsocketMsg(`httpproxy/update`);
}
export const getPac = () => {
    return sendWebsocketMsg(`httpproxy/GetPac`);
}
export const setPac = () => {
    return sendWebsocketMsg(`httpproxy/SetPac`);
}
