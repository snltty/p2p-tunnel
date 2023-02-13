import { sendWebsocketMsg } from "./request";


export const getConfig = () => {
    return sendWebsocketMsg(`configure/configure`, 'UdpForwardClientConfigure');
}
export const updateConfig = (content) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: 'UdpForwardClientConfigure', Content: content });
}

export const get = (port) => {
    return sendWebsocketMsg(`udpforward/get`, port);
}
export const getList = () => {
    return sendWebsocketMsg(`udpforward/list`);
}
export const startListen = (port) => {
    return sendWebsocketMsg(`udpforward/start`, port);
}
export const stopListen = (port) => {
    return sendWebsocketMsg(`udpforward/stop`, port);
}
export const addListen = (model) => {
    return sendWebsocketMsg(`udpforward/AddListen`, model);
}
export const removeListen = (port) => {
    return sendWebsocketMsg(`udpforward/RemoveListen`, port);
}

export const getServerPorts = () => {
    return sendWebsocketMsg(`udpforward/ServerPorts`);
}
export const getServerForwards = () => {
    return sendWebsocketMsg(`udpforward/ServerForwards`);
}

export const AddServerForward = (model) => {
    return sendWebsocketMsg(`udpforward/AddServerForward`, model);
}
export const startServerForward = (port) => {
    return sendWebsocketMsg(`udpforward/StartServerForward`, port);
}
export const stopServerForward = (port) => {
    return sendWebsocketMsg(`udpforward/StopServerForward`, port);
}
export const removeServerForward = (port) => {
    return sendWebsocketMsg(`udpforward/RemoveServerForward`, port);
}