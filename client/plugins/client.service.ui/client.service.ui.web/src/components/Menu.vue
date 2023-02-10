<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:05:47
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 22:17:12
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\components\Menu.vue
-->
<template>
    <div class="menu-wrap flex">
        <div class="logo">
            <img src="@/assets/logo.svg" @click="window.location.reload()" alt="">
        </div>
        <div class="flex-1"></div>
        <div class="navs">
            <router-link :to="{name:'Home'}">
                <el-icon>
                    <House />
                </el-icon>
                <span>首页</span>
            </router-link>
            <router-link :to="{name:'Register'}">
                <el-icon>
                    <Link />
                </el-icon>
                <span>客户端列表</span>
            </router-link>
            <router-link :to="{name:'Register'}">
                <el-icon>
                    <Position />
                </el-icon>
                <span>P2P通信</span>
            </router-link>
            <router-link :to="{name:'Register'}">
                <el-icon>
                    <OfficeBuilding />
                </el-icon>
                <span>服务器代理</span>
            </router-link>
        </div>
        <div class="flex-1"></div>
        <div class="meta">
            <!-- <a href="javascript:;" @click="editWsUrl" title="点击修改" :class="{active:websocketState.connected}">{{wsUrl}} {{connectStr}}<i class="el-icon-refresh"></i></a> -->
            <!-- <Theme></Theme> -->
        </div>
    </div>
</template>
<script>
import { computed, ref } from '@vue/reactivity';
import { onMounted } from '@vue/runtime-core'
import { injectRegister } from '../states/register'
import { injectWebsocket } from '../states/websocket'
import { initWebsocket } from '../apis/request'
import Theme from './Theme.vue'
import AuthItem from './auth/AuthItem.vue';
import { ElMessageBox } from 'element-plus'
export default {
    components: { Theme, AuthItem },
    setup () {
        const registerState = injectRegister();
        const websocketState = injectWebsocket();
        const connectStr = computed(() => `${['未连接', '已连接'][Number(websocketState.connected)]}`);
        const editWsUrl = () => {
            ElMessageBox.prompt('修改连接地址', '修改连接地址', {
                inputValue: wsUrl.value
            }).then(({ value }) => {
                localStorage.setItem('wsurl', value);
                wsUrl.value = value;
                initWebsocket(wsUrl.value);
            })
        }
        const wsUrl = ref(localStorage.getItem('wsurl') || 'ws://127.0.0.1:59411');
        onMounted(() => {
            initWebsocket(wsUrl.value);
        });

        return {
            registerState,
            websocketState, connectStr, wsUrl, editWsUrl
        }
    }
}
</script>
<style lang="stylus" scoped>
.menu-wrap
    position: relative;
    height: 5rem;
    padding-left: 1rem;
    background-color: #15602d;
    box-shadow: 1px 1px 0.6rem 0.6rem rgba(0,0,0,0.1);
    border-bottom: 1px solid #205531;
    z-index 9

.logo
    padding-top: 0.7rem;

    img
        height: 3.6rem;

.navs
    padding-top: 1.1rem;

    a
        display: inline-block;
        margin-left: 0.4rem;
        padding: 0.2rem 1rem 0.6rem 1rem;
        border-radius: 2rem;
        transition: 0.3s;
        color: #fff;
        font-size: 1.4rem;

        &.router-link-active, &:hover
            color: #15602d;
            background-color: #fff;

        .el-icon, span
            vertical-align: middle;
        .el-icon
            padding-top: 3px;
        span
            margin-left: 0.4rem;
</style>