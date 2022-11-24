/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-24 17:06:25
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\clients.js
 */
import { sendWebsocketMsg } from "./request";

export const getClients = () => {
    return sendWebsocketMsg(`clients/list`);
}

export const sendClientConnect = (id) => {
    return sendWebsocketMsg(`clients/connect`, { id: id });
}

export const sendClientConnectReverse = (id) => {
    return sendWebsocketMsg(`clients/connectreverse`, { id: id });
}
export const sendClientReset = (id) => {
    return sendWebsocketMsg(`clients/reset`, { id: id });
}
export const sendClientOffline = (id) => {
    return sendWebsocketMsg(`clients/offline`, { id: id });
}

export const sendPing = () => {
    return sendWebsocketMsg(`clients/ping`);
}


export const getConnects = () => {
    return sendWebsocketMsg(`clients/connects`);
}
export const getDelay = (paths) => {
    return sendWebsocketMsg(`clients/delay`, paths);
}
export const setRelay = (data) => {
    return sendWebsocketMsg(`clients/relay`, data);
}