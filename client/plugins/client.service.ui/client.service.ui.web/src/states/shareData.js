
/*
 * @Author: snltty
 * @Date: 2022-05-28 17:29:58
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-28 17:32:19
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\states\shareData.js
 */
import { inject, provide, reactive } from "vue";

const shareDataKey = Symbol();
export const provideShareData = () => {
    const state = reactive({
        aliveTypes: { 1: '长连接', 2: '短链接' },
        tunnelTypes: { 2: '只tcp', 4: '只udp', 8: '优先tcp', 16: '优先udp' },
    });
    provide(shareDataKey, state);
}
export const injectShareData = () => {
    return inject(shareDataKey);
}