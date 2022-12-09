
/*
 * @Author: snltty
 * @Date: 2022-05-28 17:29:58
 * @LastEditors: snltty
 * @LastEditTime: 2022-12-09 14:57:59
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\states\shareData.js
 */
import { inject, provide, reactive } from "vue";

const shareDataKey = Symbol();
export const provideShareData = () => {
    const state = reactive({
        aliveTypes: { 0: '长连接', 1: '短链接', "tunnel": 0, 'web': 1 },
        forwardTypes: { 'forward': 0, 'proxy': 1 },
        clientConnectTypes: { 0: '未连接', 1: '打洞', 2: '节点中继', 4: '服务器中继' },
        serverTypes: { 1: 'TCP', 2: 'UDP', 3: '/' },
    });
    provide(shareDataKey, state);
}
export const injectShareData = () => {
    return inject(shareDataKey);
}