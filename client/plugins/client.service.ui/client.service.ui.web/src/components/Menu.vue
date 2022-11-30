<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:05:47
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-30 17:49:19
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\components\Menu.vue
-->
<template>
    <div class="menu-wrap absolute">
        <div class="logo">
            <img src="@/assets/logo.svg" alt="">
        </div>
        <div class="copy">
            <span>@snltty、p2p-tunnel</span>
        </div>
        <div class="absolute" id="mynetwork"></div>

        <!-- <a href="javascript:;" @click="editWsUrl" title="点击修改" :class="{active:websocketState.connected}">{{wsUrl}} {{connectStr}}<i class="el-icon-refresh"></i></a> -->

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
const vis = require('vis-network');
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


        const draw = () => {
            const nodes = [
                {
                    id: 1,
                    label: "注册服务",
                    image: "./register.png",
                    shape: "image",
                    shape: "circularImage",

                },
                {
                    id: 2,
                    label: "客户端列表",
                    image: "./client.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 3,
                    label: "服务器信息",
                    image: "./server.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 4,
                    label: "虚拟网卡组网",
                    image: "./network.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 5,
                    label: "TCP转发",
                    image: "./tcp-forward.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 6,
                    label: "UDP转发",
                    image: "./udp-forward.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 7,
                    label: "HTTP代理",
                    image: "./http-proxy.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 8,
                    label: "Socks5代理",
                    image: "./socks5.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 9,
                    label: "日志信息",
                    image: "./log.png",
                    shape: "image",
                    shape: "circularImage",
                },
                {
                    id: 10,
                    label: "远程唤醒",
                    image: "./wake-up.png",
                    shape: "image",
                    shape: "circularImage",
                }
            ];
            const edges = [
                { from: 1, to: 2 },
                { from: 2, to: 3 },
                { from: 3, to: 4 },
                { from: 4, to: 5 },
                { from: 5, to: 6 },
                { from: 6, to: 7 },
                { from: 7, to: 8 },
                { from: 8, to: 9 },
                { from: 9, to: 10 },
            ];

            const container = document.getElementById("mynetwork");
            const data = { nodes: nodes, edges: edges, };
            const options = {
                interaction: { hover: true },
                nodes: {
                    shadow: true,
                    size: 30,
                    font: {
                        strokeWidth: 2, strokeColor: "rgba(0,0,0,0.2)",
                        size: 15,
                        color: "#ffffff",
                    }
                },
                edges: {
                    width: 2,
                    shadow: true,
                    color: '#fff'
                },
            };
            const network = new vis.Network(container, data, options);
            network.on("doubleClick", function (params) {
                console.log(params);
            });
        }
        onMounted(() => {
            draw();
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
.logo
    position: absolute;
    left: 1rem;
    top: 1rem;

    img
        height: 4rem;
        vertical-align: middle;

.copy
    position: absolute;
    right: 1rem;
    bottom: 1rem;
    color: #fff;
    text-shadow: 0 0 1rem rgba(0, 0, 0, 1);
</style>