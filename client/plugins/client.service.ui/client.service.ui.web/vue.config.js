module.exports = {
    productionSourceMap: process.env.NODE_ENV === 'production' ? false : true,
    outputDir: '../client.service.ui.api.service/public/web',
    publicPath: './',
    parallel: false,
    assetsDir: './',
    css: {
        extract: false
    }
}