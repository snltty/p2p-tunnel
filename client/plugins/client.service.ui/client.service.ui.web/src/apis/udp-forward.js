/*
 * @Author: snltty
 * @Date: 2021-08-21 13:58:43
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-06 15:51:00
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\udp-forward.js
 */
import { sendWebsocketMsg } from "./request";

export const get = (port) => {
    return sendWebsocketMsg(`udpforward/get`, {
        Port: port
    });
}
export const getList = () => {
    return sendWebsocketMsg(`udpforward/list`);
}
export const startListen = (port) => {
    return sendWebsocketMsg(`udpforward/start`, {
        Port: port
    });
}
export const stopListen = (port) => {
    return sendWebsocketMsg(`udpforward/stop`, {
        Port: port
    });
}
export const addListen = (model) => {
    return sendWebsocketMsg(`udpforward/AddListen`, {
        Port: model.Port,
        Content: JSON.stringify(model)
    });
}
export const removeListen = (port) => {
    return sendWebsocketMsg(`udpforward/RemoveListen`, {
        Port: port
    });
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
    return sendWebsocketMsg(`udpforward/StartServerForward`, {
        Port: port
    });
}
export const stopServerForward = (port) => {
    return sendWebsocketMsg(`udpforward/StopServerForward`, {
        Port: port
    });
}
export const removeServerForward = (port) => {
    return sendWebsocketMsg(`udpforward/RemoveServerForward`, {
        Port: port
    });
}