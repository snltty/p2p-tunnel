/*
 * @Author: snltty
 * @Date: 2021-08-21 14:57:33
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-16 18:06:31
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\ftp\list-share-data.js
 */
import { provide, inject, reactive } from "vue";

const provideFilesDataKey = Symbol();
export const provideFilesData = () => {
    const state = reactive({
        clientId: null,
        locals: [],
        remotes: []
    });
    provide(provideFilesDataKey, state);
}
export const injectFilesData = () => {
    return inject(provideFilesDataKey);
}