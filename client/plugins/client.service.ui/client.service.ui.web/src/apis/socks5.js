/*
 * @Author: snltty
 * @Date: 2021-08-21 13:58:43
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 16:00:28
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\socks5.js
 */
import { sendWebsocketMsg } from "./request";

export const get = () => {
    return sendWebsocketMsg(`socks5/get`);
}
export const set = (data) => {
    return sendWebsocketMsg(`socks5/set`, data);
}
export const getPac = () => {
    return sendWebsocketMsg(`socks5/getpac`);
}
export const updatePac = (content) => {
    return sendWebsocketMsg(`socks5/updatepac`, content);
}