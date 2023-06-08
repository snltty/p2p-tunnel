<template>
    <div class="status-bar-wrap flex">
        <div class="item">
            <Modes></Modes>
        </div>
        <div class="item">
            <Guide></Guide>
        </div>
        <div class="flex-1"></div>
        <div>
            <Websocket></Websocket>
        </div>
    </div>
</template>

<script>
import Websocket from './Websocket.vue'
import Modes from './Modes.vue'
import Guide from './Guide.vue'
export default {
    components: { Modes, Guide, Websocket },
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
    background-color: #fff;

    .item {
        margin-right: 1rem;
    }
}
</style>