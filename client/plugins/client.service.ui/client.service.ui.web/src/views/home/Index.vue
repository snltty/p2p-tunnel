<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 22:22:09
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Index.vue
-->
<template>
    <div class="home">
        <div class="connection">
            <div class="connect-button">
                <ConnectButton></ConnectButton>
            </div>
            <div class="server-line">
                <ServerLine @click="handleSelectServer"></ServerLine>
            </div>
            <div class="setting">
                <el-button type="default" color="#aaa" plain size="default">设置</el-button>
            </div>
        </div>
        <div class="servers">
            <Servers v-if="state.showServers" v-model="state.showServers"></Servers>
        </div>
        <!-- <Clients></Clients>
        <Counter v-if="registerState.LocalInfo.connected"></Counter> -->
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import { injectRegister } from '../../states/register'
import ConnectButton from './ConnectButton.vue'
import ServerLine from './ServerLine.vue'
import Servers from './Servers.vue'
export default {
    name: 'Home',
    components: { ConnectButton, ServerLine, Servers },
    setup () {

        const registerState = injectRegister();
        const state = reactive({
            showServers:false
        });

        const handleSelectServer = ()=>{
            state.showServers = true;
        }
        return {
            registerState,state,handleSelectServer
        }

    }
}
</script>
<style lang="stylus" scoped>
.connection
    padding-top: 5rem;
.connect-button,.setting
    text-align: center;
.server-line,.setting
    margin-top: 2rem;
</style>