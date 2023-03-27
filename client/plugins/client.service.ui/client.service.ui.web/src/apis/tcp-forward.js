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
    return sendWebsocketMsg(`httpproxy/listproxy`);
}
export const getPac = () => {
    return sendWebsocketMsg(`httpproxy/GetPac`);
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
