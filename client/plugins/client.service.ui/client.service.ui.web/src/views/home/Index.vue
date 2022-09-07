<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-30 22:38:45
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Index.vue
-->
<template>
    <div class="home">
        <Clients></Clients>
        <Counter v-if="registerState.LocalInfo.connected"></Counter>
    </div>
</template>

<script>
import Clients from './Clients.vue'
import Counter from './Counter.vue'
import { injectRegister } from '../../states/register'
export default {
    name: 'Home',
    components: { Clients, Counter },
    setup () {

        const registerState = injectRegister();

        return {
            registerState
        }

    }
}
</script>
<style lang="stylus" scoped>
.home
    padding: 2rem;
</style>