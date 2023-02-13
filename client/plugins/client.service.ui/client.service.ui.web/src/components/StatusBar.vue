<template>
    <div class="status-bar-wrap flex">
        <div class="flex-1"></div>
        <div>
            <Websocket></Websocket>
        </div>
    </div>
</template>

<script>
import Websocket from './Websocket.vue'
export default {
    components: { Websocket },
    setup() {
        return {}
    }
}
</script>

<style lang="stylus" scoped>
.status-bar-wrap {
    border-top: 1px solid #ddd;
    padding: 0 0.6rem;
    height: 3rem;
    line-height: 3rem;
}
</style>