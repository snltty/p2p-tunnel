/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2022-12-05 17:36:50
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\configure.js
 */
import { sendWebsocketMsg } from "./request";

export const getConfigures = () => {
    return sendWebsocketMsg(`configure/configures`);
}

export const getConfigure = (className) => {
    return sendWebsocketMsg(`configure/configure`, className);
}
export const saveConfigure = (className, content) => {
    return sendWebsocketMsg(`configure/save`, { ClassName: className, Content: content });
}

export const enableConfigure = (className, enable) => {
    return sendWebsocketMsg(`configure/enable`, { ClassName: className, Enable: enable });
}

export const getServices = () => {
    return sendWebsocketMsg(`configure/services`);
}

