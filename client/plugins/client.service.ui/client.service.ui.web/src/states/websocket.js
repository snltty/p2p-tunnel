/*
 * @Author: snltty
 * @Date: 2021-08-20 00:33:24
 * @LastEditors: snltty
 * @LastEditTime: 2021-08-20 00:42:41
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\states\websocket.js
 */
import { provide, inject, reactive } from "vue";
import { subWebsocketState } from '../apis/request'

const provideWebsocketKey = Symbol();
export const provideWebsocket = () => {
    const state = reactive({
        connected: false
    });
    provide(provideWebsocketKey, state);

    subWebsocketState((connected) => {
        state.connected = connected;
    });
}
export const injectWebsocket = () => {
    return inject(provideWebsocketKey);
}