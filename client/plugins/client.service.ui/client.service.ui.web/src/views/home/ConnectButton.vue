<!--
 * @Author: snltty
 * @Date: 2023-02-10 21:02:51
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 22:27:08
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\ConnectButton.vue
-->
<template>
    <div>
        <a href="javascript:;" :class="className">
            <template v-if="loading">
                <el-icon :size="30" class="is-loading">
                    <Loading />
                </el-icon>
            </template>
            <template v-else>
                <el-icon :size="30">
                    <SwitchButton />
                </el-icon>
            </template>
        </a>
    </div>
</template>

<script>
import { computed, ref } from '@vue/reactivity'
export default {
    setup () {

        const loading = computed(() => false);
        const connected = computed(() => false);
        const className = computed(() => connected.value ? 'green' : 'gray');
        
        return {
            loading, connected, className
        }
    }
}
</script>

<style lang="stylus" scoped>
a
    width: 15rem;
    height: 15rem;
    border-radius: 50%;
    text-decoration: none !important;
    white-space: nowrap;
    display: inline-block;
    vertical-align: baseline;
    position: relative;
    cursor: pointer;
    background-repeat: no-repeat;
    background-position: bottom left;
    background-image: url('../../assets/button_bg.png');
    background-position: bottom left, top right, 0 0, 0 0;
    background-clip: border-box;
    box-shadow: 0 0 1px #fff inset;
    -webkit-transition: background-position 1s;
    -moz-transition: background-position 1s;
    transition: background-position 1s, box-shadow 0.3s;

    &.gray
        box-shadow: 0 0 0 8px #d1d8e261;
        color: #525252 !important;
        border: 1px solid #dde1dd !important;
        background-color: #a9adb1;
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -moz-radial-gradient(center bottom, circle, rgba(197, 199, 202, 1) 0, rgba(197, 199, 202, 0) 100px), -moz-linear-gradient(#c5c7ca, #92989c);
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(#fefeff), to(rgba(197, 199, 202, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#e1e4e8), to(#b4bcc2));

        .el-icon
            color: #abb1b7;

    &.gray:hover
        box-shadow: 0 0 0 12px #d1d8e261;
        background-color: #b6bbc0;
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -moz-radial-gradient(center bottom, circle, rgba(202, 205, 208, 1) 0, rgba(202, 205, 208, 0) 100px), -moz-linear-gradient(#d1d3d6, #9fa5a9);
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(#fefeff), to(rgba(197, 199, 202, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#e1e4e8), to(#b4bcc2));

    &.green
        box-shadow: 0 0 0 8px #aee5b696;
        color: #525252 !important;
        border: 1px solid #78c18a !important;
        background-color: #79be1e;
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -moz-radial-gradient(center bottom, circle, rgba(162, 211, 30, 1) 0, rgba(162, 211, 30, 0) 100px), -moz-linear-gradient(#82cc27, #74b317);
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(rgba(162, 211, 30, 1)), to(rgba(162, 211, 30, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#82cc27), to(#74b317));

        .el-icon
            color: #43873f;

    &.green:hover
        box-shadow: 0 0 0 12px #aee5b696;
        background-color: #89d228;
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -moz-radial-gradient(center bottom, circle, rgba(183, 229, 45, 1) 0, rgba(183, 229, 45, 0) 100px), -moz-linear-gradient(#90de31, #7fc01e);
        background-image: url('../../assets/button_bg.png'), url('../../assets/button_bg.png'), -webkit-gradient(radial, 50% 100%, 0, 50% 100%, 100, from(rgba(183, 229, 45, 1)), to(rgba(183, 229, 45, 0))), -webkit-gradient(linear, 0% 0%, 0% 100%, from(#90de31), to(#7fc01e));

    &:hover
        background-position: top left;
        background-position: top left, bottom right, 0 0, 0 0;

    &:active
        bottom: -1px;

    .el-icon
        font-weight: bold;
        margin-top: 6rem;
        color: #abb1b7;
</style>