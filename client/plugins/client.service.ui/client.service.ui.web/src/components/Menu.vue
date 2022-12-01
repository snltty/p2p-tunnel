<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:05:47
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-18 12:59:51
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\components\Menu.vue
-->
<template>
    <div class="menu-wrap flex">
        <div class="logo">
            <img src="@/assets/logo.svg" alt="">
        </div>
        <div class="navs flex-1">
            <div class="hidden-xs-only">
                <router-link :to="{name:'Home'}">首页</router-link>
                <router-link :to="{name:'Register'}">注册服务 <i class="el-icon-circle-check" :class="{active:registerState.LocalInfo.TcpConnected}"></i></router-link>
                <el-dropdown size="small">
                    <span class="el-dropdown-link">
                        <span>客户端应用</span>
                        <span>{{serviceRouteName}}</span>
                        <el-icon size="30">
                            <ArrowDown></ArrowDown>
                        </el-icon>
                    </span>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <template v-if="websocketState.connected">
                                <template v-for="(item,index) in servicesMenus" :key="index">
                                    <auth-item :name="item.service">
                                        <el-dropdown-item>
                                            <router-link :to="{name:item.name}">{{item.text}}</router-link>
                                        </el-dropdown-item>
                                    </auth-item>
                                </template>
                            </template>
                            <template v-else>
                                <template v-for="(item,index) in servicesMenus" :key="index">
                                    <el-dropdown-item>
                                        <router-link :to="{name:item.name}" class="disabled">{{item.text}}</router-link>
                                    </el-dropdown-item>
                                </template>
                            </template>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
                <el-dropdown size="small">
                    <span class="el-dropdown-link">
                        <span>服务端应用</span>
                        <span>{{serverRouteName}}</span>
                        <el-icon size="30">
                            <ArrowDown></ArrowDown>
                        </el-icon>
                    </span>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <template v-if="websocketState.connected">
                                <template v-for="(item,index) in serverMenus" :key="index">
                                    <auth-item :name="item.service">
                                        <el-dropdown-item>
                                            <router-link :to="{name:item.name}">{{item.text}}</router-link>
                                        </el-dropdown-item>
                                    </auth-item>
                                </template>
                            </template>
                            <template v-else>
                                <template v-for="(item,index) in serverMenus" :key="index">
                                    <el-dropdown-item>
                                        <router-link :to="{name:item.name}" class="disabled">{{item.text}}</router-link>
                                    </el-dropdown-item>
                                </template>
                            </template>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
        </div>
        <div class="meta">
            <a href="javascript:;" @click="editWsUrl" title="点击修改" :class="{active:websocketState.connected}">{{wsUrl}} {{connectStr}}<i class="el-icon-refresh"></i></a>
            <!-- <Theme></Theme> -->
        </div>
    </div>
</template>
<script>
import { computed, ref } from '@vue/reactivity';
import { onMounted } from '@vue/runtime-core'
import { useRoute, useRouter } from 'vue-router'
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

        const router = useRouter();
        const route = useRoute();
        const serviceRouteName = computed(() => {
            if (route.matched.length > 0 && route.matched[0].name == 'Services') {
                return `-${route.meta.name}`;
            }
            return '';
        });
        const servicesMenus = router.options.routes.filter(c => c.name == 'Services')[0].children.map(c => {
            return {
                name: c.name,
                text: c.meta.name,
                service: c.meta.service,
            }
        });

        const serverRouteName = computed(() => {
            if (route.matched.length > 0 && route.matched[0].name == 'Server') {
                return `-${route.meta.name}`;
            }
            return '';
        });
        const serverMenus = router.options.routes.filter(c => c.name == 'Server')[0].children.map(c => {
            return {
                name: c.name,
                text: c.meta.name,
                service: c.meta.service,
            }
        });

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
            serviceRouteName, servicesMenus,
            serverRouteName, serverMenus,
            websocketState, connectStr, wsUrl, editWsUrl
        }
    }
}
</script>
<style lang="stylus" scoped>
.menu-wrap
    line-height: 8rem;
    height: 8rem;

.el-dropdown-menu__item
    padding: 0;
    line-height: normal;

    &:hover
        background-color: rgba(0, 0, 0, 0.1) !important;

    a
        padding: 0 2rem;
        line-height: 3.6rem;
        display: block;

        &.disabled
            color: #888;

.logo
    img
        height: 4rem;
        vertical-align: middle;

.navs
    padding-left: 2rem;

    .el-dropdown
        vertical-align: inherit;

    a, .el-dropdown-link
        margin-left: 0.4rem;
        padding: 0.6rem 1rem;
        border-radius: 0.4rem;
        transition: 0.3s;
        transition: 0.3s;
        color: #fff;
        text-shadow: 0 1px 1px #28866e;
        font-size: 1.4rem;

        &.router-link-active, &:hover
            color: #fff;
            background-color: rgba(0, 0, 0, 0.5);

    i
        // opacity: 0.5;
        vertical-align: middle;

i.active
    color: #10da10;
    opacity: 1;

.meta
    a
        color: #fff700;

        &.active
            color: #5bff68;
</style>