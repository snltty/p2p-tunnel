import { sendWebsocketMsg } from "./request";

export const getServerPorts = () => {
    return sendWebsocketMsg(`servertcpforward/Ports`);
}
export const getServerForwards = () => {
    return sendWebsocketMsg(`servertcpforward/List`);
}

export const AddServerForward = (model) => {
    return sendWebsocketMsg(`servertcpforward/Add`, model);
}
export const startServerForward = (model) => {
    return sendWebsocketMsg(`servertcpforward/Start`, model);
}
export const stopServerForward = (model) => {
    return sendWebsocketMsg(`servertcpforward/Stop`, model);
}
export const removeServerForward = (model) => {
    return sendWebsocketMsg(`servertcpforward/Remove`, model);
}