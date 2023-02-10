<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 17:59:43
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\App.vue
-->
<template>
    <el-config-provider :locale="locale" size="large">
        <auth-wrap>
            <Background></Background>
            <div class="body absolute">
                <div class="wrap absolute flex flex-column flex-nowrap h-100">
                    <div class="menu">
                        <Menu></Menu>
                    </div>
                    <div class="content flex-1 relative scrollbar-10">
                        <router-view />
                    </div>
                    <!-- <Foot></Foot> -->
                </div>
            </div>
        </auth-wrap>
    </el-config-provider>
</template>
<script>
import Menu from './components/Menu.vue'
import Foot from './components/Foot.vue'
import Background from './components/Background.vue'
import { provideRegister } from './states/register'
import { provideWebsocket } from './states/websocket'
import { provideClients } from './states/clients'
import { provideShareData } from './states/shareData'
import { ElConfigProvider } from 'element-plus'
import zhCn from 'element-plus/lib/locale/lang/zh-cn'
export default {
    components: { Menu, Foot, Background, ElConfigProvider },
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
    z-index 9
.wrap
    width: 100%;
    height: 60rem;
    max-width: 80rem;
    max-height: 100%;
    background-color: #fff;
    left: 50%;
    top: 50%;
    transform: translateX(-50%) translateY(-50%);

.content
    border-radius: 0.4rem;
</style>
