/*
 * @Author: snltty
 * @Date: 2021-08-22 00:28:31
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-11 15:41:25
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\vue.config.js
 */
module.exports = {
    productionSourceMap: process.env.NODE_ENV === 'production' ? false : true,
    outputDir: '../client.service.ui.api.service/public/web',
    publicPath: './',
    parallel: false,
    assetsDir: './',
    configureWebpack: (config) => {
        config.module.rules.push({
            test: /\.md$/,
            use: [{
                loader: 'raw-loader',
            }]
        })
    }
}