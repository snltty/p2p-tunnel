<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 15:32:36
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Index.vue
-->
<template>
    <div class="home">
        <div class="connect-button">
            <ConnectButton></ConnectButton>
        </div>
        <div class="server-line">
            <ServerLine></ServerLine>
        </div>
        <!-- <Clients></Clients>
        <Counter v-if="registerState.LocalInfo.connected"></Counter> -->
    </div>
</template>

<script>
import { injectRegister } from '../../states/register'
import ConnectButton from './ConnectButton.vue'
import ServerLine from './ServerLine.vue'
export default {
    name: 'Home',
    components: { ConnectButton, ServerLine },
    setup () {

        const registerState = injectRegister();

        return {
            registerState
        }

    }
}
</script>
<style lang="stylus" scoped>
.connect-button
    padding: 5rem 0 2rem 0;
    text-align: center;
</style>