/*
 * @Author: snltty
 * @Date: 2021-09-26 20:43:28
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-23 17:50:51
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\extends\number.js
 */
Number.prototype.sizeFormat = function () {
    let unites = ['B', 'KB', 'MB', 'GB', 'TB'];
    let unit = unites[0], size = this;
    while ((unit = unites.shift()) && size > 1024) {
        size /= 1024;
    }
    return unit == 'B' ? [size, unit] : [size.toFixed(2), unit];
}

const add0 = (num) => {
    return num < 10 ? '0' + num : num;
}

Number.prototype.timeFormat = function () {
    let num = this;
    return [
        add0(Math.floor(num / 60 / 60 / 24)),
        add0(Math.floor(num / 60 / 60 % 24)),
        add0(Math.floor(num / 60 % 60)),
        add0(Math.floor(num % 60)),
    ];
}