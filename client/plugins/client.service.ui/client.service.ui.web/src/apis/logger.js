/*
 * @Author: snltty
 * @Date: 2021-10-02 14:20:20
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-14 21:00:13
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\logger.js
 */
import { sendWebsocketMsg } from "./request";

export const getLoggers = (page) => {
    return sendWebsocketMsg(`logger/list`, page);
}
export const clearLoggers = () => {
    return sendWebsocketMsg(`logger/clear`);
}