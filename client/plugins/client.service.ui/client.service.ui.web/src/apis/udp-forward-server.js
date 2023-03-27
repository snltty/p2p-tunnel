import { sendWebsocketMsg } from "./request";

export const getServerPorts = () => {
    return sendWebsocketMsg(`serverudpforward/Ports`);
}
export const getServerForwards = () => {
    return sendWebsocketMsg(`serverudpforward/List`);
}

export const AddServerForward = (model) => {
    return sendWebsocketMsg(`serverudpforward/Add`, model);
}
export const startServerForward = (port) => {
    return sendWebsocketMsg(`serverudpforward/Start`, port);
}
export const stopServerForward = (port) => {
    return sendWebsocketMsg(`serverudpforward/Stop`, port);
}
export const removeServerForward = (port) => {
    return sendWebsocketMsg(`serverudpforward/Remove`, port);
}