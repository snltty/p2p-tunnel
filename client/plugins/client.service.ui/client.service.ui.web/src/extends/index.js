/*
 * @Author: snltty
 * @Date: 2021-08-20 16:17:00
 * @LastEditors: snltty
 * @LastEditTime: 2021-08-20 16:17:00
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\extends\index.js
 */
const files = require.context('.', false, /\.js$/);
files.keys().forEach(key => {
    if (key == './index.js') return;
    files(key).default;
});