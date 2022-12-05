/*
 * @Author: snltty
 * @Date: 2021-08-21 13:58:43
 * @LastEditors: snltty
 * @LastEditTime: 2022-12-05 11:19:19
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\vea.js
 */
import { sendWebsocketMsg } from "./request";

export const getConfig = () => {
    return sendWebsocketMsg(`vea/get`);
}
export const setConfig = (data) => {
    return sendWebsocketMsg(`vea/set`, data);
}
export const getList = () => {
    return sendWebsocketMsg(`vea/list`);
}
export const reset = (id) => {
    return sendWebsocketMsg(`vea/reset`, id);
}
export const update = () => {
    return sendWebsocketMsg(`vea/update`);
}