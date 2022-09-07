/*
 * @Author: xr
 * @Date: 2021-03-23 21:08:53
 * @LastEditors: xr
 * @LastEditTime: 2021-03-23 21:41:30
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \admin-ui\src\components\menu\index.js
 */
import AuthItem from './AuthItem.vue';
import AuthWrap from './AuthWrap.vue';
export default {
    install: (app) => {
        app.component('AuthWrap', AuthWrap);
        app.component('AuthItem', AuthItem);
    }
}