import { sendWebsocketMsg } from "./request";

export const getServerPorts = () => {
    return sendWebsocketMsg(`serverforward/Ports`);
}
export const getServerForwards = () => {
    return sendWebsocketMsg(`serverforward/List`);
}

export const AddServerForward = (model) => {
    return sendWebsocketMsg(`serverforward/Add`, model);
}
export const startServerForward = (model) => {
    return sendWebsocketMsg(`serverforward/Start`, model);
}
export const stopServerForward = (model) => {
    return sendWebsocketMsg(`serverforward/Stop`, model);
}
export const removeServerForward = (model) => {
    return sendWebsocketMsg(`serverforward/Remove`, model);
}