import { sendWebsocketMsg } from "./request";

export const getConfig = () => {
    return sendWebsocketMsg(`configure/configure`, 'TcpForwardClientConfigure');
}
export const updateConfig = (content) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: 'TcpForwardClientConfigure', Content: content });
}

export const get = (id) => {
    return sendWebsocketMsg(`tcpforward/get`, id);
}
export const getList = () => {
    return sendWebsocketMsg(`tcpforward/list`);
}
export const getListProxy = () => {
    return sendWebsocketMsg(`tcpforward/listproxy`);
}
export const getPac = () => {
    return sendWebsocketMsg(`tcpforward/GetPac`);
}

export const startListen = (id) => {
    return sendWebsocketMsg(`tcpforward/start`, id);
}

export const stopListen = (id) => {
    return sendWebsocketMsg(`tcpforward/stop`, id);
}

export const addListen = (model) => {
    return sendWebsocketMsg(`tcpforward/AddListen`, model);
}
export const removeListen = (id) => {
    return sendWebsocketMsg(`tcpforward/RemoveListen`, id);
}

export const addForward = (model) => {
    return sendWebsocketMsg(`tcpforward/AddForward`, model);
}
export const removeForward = (listenid, forwardid) => {
    return sendWebsocketMsg(`tcpforward/RemoveForward`, {
        ListenID: listenid,
        ForwardID: forwardid
    });
}


export const getServerPorts = () => {
    return sendWebsocketMsg(`tcpforward/ServerPorts`);
}
export const getServerForwards = () => {
    return sendWebsocketMsg(`tcpforward/ServerForwards`);
}

export const AddServerForward = (model) => {
    return sendWebsocketMsg(`tcpforward/AddServerForward`, model);
}
export const startServerForward = (model) => {
    return sendWebsocketMsg(`tcpforward/StartServerForward`, model);
}
export const stopServerForward = (model) => {
    return sendWebsocketMsg(`tcpforward/StopServerForward`, model);
}
export const removeServerForward = (model) => {
    return sendWebsocketMsg(`tcpforward/RemoveServerForward`, model);
}