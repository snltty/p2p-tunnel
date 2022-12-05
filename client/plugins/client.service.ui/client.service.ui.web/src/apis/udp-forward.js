/*
 * @Author: snltty
 * @Date: 2021-08-21 13:58:43
 * @LastEditors: snltty
 * @LastEditTime: 2022-12-05 09:53:21
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\udp-forward.js
 */
import { sendWebsocketMsg } from "./request";

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