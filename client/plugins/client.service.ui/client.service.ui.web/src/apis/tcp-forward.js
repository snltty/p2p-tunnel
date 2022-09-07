/*
 * @Author: snltty
 * @Date: 2021-08-21 13:58:43
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-06 15:42:26
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\tcp-forward.js
 */
import { sendWebsocketMsg } from "./request";

export const get = (id) => {
    return sendWebsocketMsg(`tcpforward/get`, {
        ID: id
    });
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
    return sendWebsocketMsg(`tcpforward/start`, {
        ID: id
    });
}

export const stopListen = (id) => {
    return sendWebsocketMsg(`tcpforward/stop`, {
        ID: id
    });
}

export const addListen = (model) => {
    return sendWebsocketMsg(`tcpforward/AddListen`, {
        ID: model.ID,
        Content: JSON.stringify(model)
    });
}
export const removeListen = (id) => {
    return sendWebsocketMsg(`tcpforward/RemoveListen`, {
        ID: id
    });
}

export const addForward = (model) => {
    return sendWebsocketMsg(`tcpforward/AddForward`, {
        ID: model.Forward.ID,
        Content: JSON.stringify(model)
    });
}
export const removeForward = (listenid, forwardid) => {
    return sendWebsocketMsg(`tcpforward/RemoveForward`, {
        ID: forwardid,
        Content: JSON.stringify({
            ListenID: listenid,
            ForwardID: forwardid
        })
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