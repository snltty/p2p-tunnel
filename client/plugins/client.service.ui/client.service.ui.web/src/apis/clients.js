import { sendWebsocketMsg } from "./request";

export const getClients = () => {
    return sendWebsocketMsg(`clients/list`);
}

export const sendClientConnect = (id) => {
    return sendWebsocketMsg(`clients/connect`, id);
}

export const sendClientConnectReverse = (id) => {
    return sendWebsocketMsg(`clients/connectreverse`, id);
}
export const sendClientReset = (id) => {
    return sendWebsocketMsg(`clients/reset`, id);
}
export const sendClientOffline = (id) => {
    return sendWebsocketMsg(`clients/offline`, id);
}

export const sendPing = () => {
    return sendWebsocketMsg(`clients/ping`);
}


export const getConnects = () => {
    return sendWebsocketMsg(`clients/connects`);
}
export const getDelay = (relayids) => {
    return sendWebsocketMsg(`clients/delay`, relayids);
}
export const setRelay = (relayids) => {
    return sendWebsocketMsg(`clients/relay`, relayids);
}