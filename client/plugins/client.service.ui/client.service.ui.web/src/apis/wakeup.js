/*
 * @Author: snltty
 * @Date: 2021-08-21 13:58:43
 * @LastEditors: snltty
 * @LastEditTime: 2022-10-01 17:49:41
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\wakeup.js
 */
import { sendWebsocketMsg } from "./request";

export const getConfig = () => {
    return sendWebsocketMsg(`wakeup/get`);
}
export const getUpdate = () => {
    return sendWebsocketMsg(`wakeup/update`);
}

export const wakeup = (data) => {
    return sendWebsocketMsg(`wakeup/wakeup`, data);
}