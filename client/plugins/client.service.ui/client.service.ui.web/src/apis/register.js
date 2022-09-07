/*
 * @Author: snltty
 * @Date: 2021-08-19 23:30:00
 * @LastEditors: snltty
 * @LastEditTime: 2022-07-12 13:24:02
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\register.js
 */
import { sendWebsocketMsg } from "./request";

export const sendRegisterMsg = () => {
    return sendWebsocketMsg(`register/start`);
}

export const getRegisterInfo = () => {
    return sendWebsocketMsg(`register/info`);
}

export const updateConfig = (config) => {
    return sendWebsocketMsg(`register/config`, config);
}