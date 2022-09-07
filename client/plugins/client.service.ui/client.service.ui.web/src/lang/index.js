/*
 * @Author: snltty
 * @Date: 2021-08-20 16:33:56
 * @LastEditors: snltty
 * @LastEditTime: 2021-08-20 16:33:57
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\langs\index.js
 */
const files = require.context('.', false, /\.js$/);
const modules = {};
files.keys().forEach(key => {
    if (key == './index.js') return;
    modules[key.replace(/(\.\/|\.js)/g, '')] = files(key).default
});
export default modules;
