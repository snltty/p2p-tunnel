/*
 * @Author: snltty
 * @Date: 2021-08-21 14:58:34
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-20 16:35:06
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\test.js
 */
import { sendWebsocketMsg } from "./request";
export const sendPacketTest = (id, count, kb) => {
    return sendWebsocketMsg(`test/packet`, { id: +id, count: +count, kb: +kb });
}