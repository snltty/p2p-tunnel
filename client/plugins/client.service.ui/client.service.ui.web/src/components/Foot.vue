<!--
 * @Author: snltty
 * @Date: 2022-08-18 13:02:18
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-30 22:39:08
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\components\Foot.vue
-->
<template>
    <div class="copyright">
        <span>@snltty、p2p-tunnel</span>
        <div class="menu-wrap hidden-sm-and-up">
            <el-row>
                <el-col :span="8">
                    <router-link :to="{name:'Home'}">首页</router-link>
                </el-col>
                <el-col :span="8">
                    <router-link :to="{name:'Register'}">注册服务 <i class="el-icon-circle-check" :class="{active:registerState.LocalInfo.connected}"></i></router-link>
                </el-col>
                <el-col :span="8">
                    <el-dropdown size="small">
                        <span class="el-dropdown-link">
                            <span>插件应用</span>
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
                </el-col>
            </el-row>
        </div>
    </div>
</template>

<script>
import { injectRegister } from '../states/register'
import { injectWebsocket } from '../states/websocket'
import { useRoute, useRouter } from 'vue-router'
export default {
    setup () {
        const registerState = injectRegister();
        const websocketState = injectWebsocket();
        const router = useRouter();

        const servicesMenus = router.options.routes.filter(c => c.name == 'Services')[0].children.map(c => {
            return {
                name: c.name,
                text: c.meta.name,
                service: c.meta.service,
            }
        }).concat(router.options.routes.filter(c => c.name == 'Server')[0].children.map(c => {
            return {
                name: c.name,
                text: c.meta.name,
                service: c.meta.service,
            }
        }));


        return {
            registerState, websocketState, servicesMenus
        }
    }
}
</script>

<style lang="stylus" scoped>
.copyright
    padding: 2rem 0;
    text-align: center;
    color: #fff;

@media screen and (max-width: 768px)
    .copyright
        padding: 1rem 0 5rem 0;

.menu-wrap
    position: fixed;
    left: 0;
    right: 0;
    bottom: 0;
    height: 5rem;
    line-height: 5rem;
    background-color: #fff;
    border-top: 1px solid #ddd;
    font-size: 1.4rem;
    box-shadow: -1px -1px 8px rgba(0, 0, 0, 0.1);

    .el-dropdown
        vertical-align: inherit;

.el-dropdown-menu
    a
        display: block;
        font-size: 1.4rem;
        padding: 1rem 0;
        width: 100%;
</style>