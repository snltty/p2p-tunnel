<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-30 16:30:02
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\App.vue
-->
<template>
    <el-config-provider :locale="locale" size="large">
        <auth-wrap>
            <div class="body absolute">
                <Menu></Menu>
                <!-- <div class="wrap flex flex-column flex-nowrap h-100">
                    <div class="menu">
                       
                    </div>
                    <div class="content flex-1 relative scrollbar-10">
                        <router-view />
                    </div>
                    <Foot></Foot>
                </div> -->
            </div>
        </auth-wrap>
    </el-config-provider>
</template>
<script>
import Menu from './components/Menu.vue'
import Foot from './components/Foot.vue'
import { provideRegister } from './states/register'
import { provideWebsocket } from './states/websocket'
import { provideClients } from './states/clients'
import { provideShareData } from './states/shareData'
import { ElConfigProvider } from 'element-plus'
import zhCn from 'element-plus/lib/locale/lang/zh-cn'

export default {
    components: { Menu, Foot, ElConfigProvider },
    setup () {
        provideRegister();
        provideWebsocket();
        provideClients();
        provideShareData();
        return {
            locale: zhCn
        }
    }
}
</script>
<style lang="stylus" scoped>
.body
    background-image: url('./assets/bg.webp');
    background-size: 100% 100%;
    background-position: 0 0;
    background-repeat: no-repeat;
</style>
