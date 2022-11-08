/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-08 11:11:35
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
    return sendWebsocketMsg(`clients/Reset`, { id: id });
}

export const sendPing = () => {
    return sendWebsocketMsg(`clients/Ping`);
}

export const getDelay = (tid) => {
    return sendWebsocketMsg(`clients/delay`, tid);
}
export const setRelay = (data) => {
    return sendWebsocketMsg(`clients/relay`, data);
}