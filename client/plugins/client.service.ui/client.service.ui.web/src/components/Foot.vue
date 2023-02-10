<!--
 * @Author: snltty
 * @Date: 2022-08-18 13:02:18
 * @LastEditors: snltty
 * @LastEditTime: 2022-12-04 17:53:05
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\components\Foot.vue
-->
<template>
</template>

<script>
import { injectRegister } from '../states/register'
import { injectWebsocket } from '../states/websocket'
export default {
    setup () {
        const registerState = injectRegister();
        const websocketState = injectWebsocket();
        return {
            registerState, websocketState
        }
    }
}
</script>

<style lang="stylus" scoped></style>