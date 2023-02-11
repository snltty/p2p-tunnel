<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 01:48:56
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
                <ServerLine ref="serverLineDom" @handle="handleSelectServer"></ServerLine>
            </div>
        </div>
        <div class="servers">
            <Servers v-if="state.showServers" v-model="state.showServers" @success="handleSelectServerSuccess"></Servers>
        </div>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity'
import { injectRegister } from '../../states/register'
import { sendExit } from '../../apis/register'
import ConnectButton from './ConnectButton.vue'
import ServerLine from './ServerLine.vue'
import Servers from './Servers.vue'
export default {
    name: 'Home',
    components: { ConnectButton, ServerLine, Servers },
    setup () {

        const registerState = injectRegister();
        const state = reactive({
            showServers:false,
        });

        const serverLineDom = ref(null);
        const handleSelectServer = ()=>{
            state.showServers = true;
        }
        const handleSelectServerSuccess = ()=>{
            sendExit();
            serverLineDom.value.update();
        }

        return {
            registerState,state,serverLineDom,handleSelectServer,handleSelectServerSuccess
        }

    }
}
</script>
<style lang="stylus" scoped>
.connection
    padding-top: 5rem;
.connect-button
    text-align: center;
.server-line
    margin-top: 2rem;
</style>