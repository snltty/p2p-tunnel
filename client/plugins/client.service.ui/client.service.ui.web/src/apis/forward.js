import { sendWebsocketMsg } from "./request";

export const getConfig = () => {
    return sendWebsocketMsg(`configure/configure`, 'ForwardClientConfigure');
}
export const updateConfig = (content) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: 'ForwardClientConfigure', Content: content });
}

export const get = (id) => {
    return sendWebsocketMsg(`forward/get`, id);
}
export const getList = () => {
    return sendWebsocketMsg(`forward/list`);
}

export const startListen = (id) => {
    return sendWebsocketMsg(`forward/start`, id);
}

export const stopListen = (id) => {
    return sendWebsocketMsg(`forward/stop`, id);
}

export const addListen = (model) => {
    return sendWebsocketMsg(`forward/AddListen`, model);
}
export const removeListen = (id) => {
    return sendWebsocketMsg(`forward/RemoveListen`, id);
}

export const addForward = (model) => {
    return sendWebsocketMsg(`forward/AddForward`, model);
}
export const removeForward = (listenid, forwardid) => {
    return sendWebsocketMsg(`forward/RemoveForward`, {
        ListenID: listenid,
        ForwardID: forwardid
    });
}