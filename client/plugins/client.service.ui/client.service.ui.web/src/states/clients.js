/*
 * @Author: snltty
 * @Date: 2021-08-21 14:57:33
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-11 14:29:46
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\states\clients.js
 */
import { provide, inject, reactive } from "vue";
import { websocketState } from '../apis/request'
import { getClients } from '../apis/clients'

const provideClientsKey = Symbol();
export const provideClients = () => {
    const state = reactive({
        clients: []
    });
    provide(provideClientsKey, state);

    setInterval(() => {
        if (websocketState.connected) {
            getClients().then((res) => {
                state.clients = res;
            })
        } else {
            state.clients = [];
        }
    }, 1000);
}
export const injectClients = () => {
    return inject(provideClientsKey);
}