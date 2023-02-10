<!--
 * @Author: snltty
 * @Date: 2023-02-10 21:02:51
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 21:57:36
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\ServerLine.vue
-->
<template>
    <div>
        <div class="line flex" title="选择服务器线路">
            <div class="country-img">
                <img src="../../assets/zg.png">
            </div>
            <div class="country-name">中国</div>
            <div class="flex-1"></div>
            <div class="country-time">
                <Signal :value="30"></Signal>
            </div>
            <div class="country-select">
                <el-icon>
                    <ArrowRightBold />
                </el-icon>
            </div>
        </div>
    </div>
</template>

<script>
//http://icon.mobanwang.com/2010/467.html
import Signal from './Signal.vue'
export default {
    components: { Signal },
    setup () {
        return {}
    }
}
</script>

<style lang="stylus" scoped>
.line
    cursor: pointer;
    margin: 0 auto;
    border: 1px solid #ddd;
    width: 20rem;
    padding: 0.6rem;
    border-radius: 0.4rem;
    transition: 0.3s;

    &:hover
        box-shadow: 0 0 0 0.4rem #d1d8e261;

    .country-img
        font-size: 0;
        margin-right: 0.6rem;

        img
            height: 2rem;

    .country-name
        line-height: 2rem;

    .country-time, .country-select
        padding-top: 0.2rem;

    .country-select
        margin-left: 0.6rem;
        color: #999;
</style>