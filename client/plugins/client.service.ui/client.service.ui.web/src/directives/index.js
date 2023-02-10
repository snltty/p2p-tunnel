/*
 * @Author: snltty
 * @Date: 2023-02-10 21:29:06
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 21:34:00
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\directives\index.js
 */
const files = require.context('.', false, /\.js$/);
const fn = (app)=>{
    files.keys().forEach(key => {
        if (key == './index.js') return;
        files(key).default(app);
    });
}
export default fn;