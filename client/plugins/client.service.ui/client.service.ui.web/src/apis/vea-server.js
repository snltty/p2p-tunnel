import { sendWebsocketMsg } from "./request";

export const getConfig = () => {
    return sendWebsocketMsg(`servervea/list`);
}
export const addNetwork = (ip) => {
    return sendWebsocketMsg(`servervea/addnetwork`, ip);
}
export const modifyIP = (connectionid, ip) => {
    return sendWebsocketMsg(`servervea/modifyIP`, {
        ConnectionId: +connectionid,
        IP: +ip
    });
}
export const deleteIP = (connectionid) => {
    return sendWebsocketMsg(`servervea/deleteIP`, connectionid);
}