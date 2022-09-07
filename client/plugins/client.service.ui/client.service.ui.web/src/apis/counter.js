/*
 * @Author: snltty
 * @Date: 2022-04-12 15:52:08
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-12 16:38:09
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\apis\counter.js
 */
/*
 * @Author: snltty
 * @Date: 2021-08-20 11:00:08
 * @LastEditors: xr
 * @LastEditTime: 2022-01-23 13:57:23
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\apis\config.js
 */
import { sendWebsocketMsg } from "./request";

export const getCounter = () => {
    return sendWebsocketMsg(`counter/info`, {}, true);
}