<template>
    <div>
        <a href="javascript:;" :class="{connected:connected}" @click="editWsUrl">{{wsUrl}}</a>
    </div>
</template>

<script>
import { injectWebsocket } from '../states/websocket'
import { initWebsocket } from '../apis/request'
import { computed, onMounted, ref } from '@vue/runtime-core'
import { ElMessageBox } from 'element-plus'
export default {
    components: {},
    setup() {

        const websocketState = injectWebsocket();
        const connected = computed(() => websocketState.connected);
        const editWsUrl = () => {
            ElMessageBox.prompt('修改连接地址', '修改连接地址', {
                inputValue: wsUrl.value,
                confirmButtonText: '确定',
                cancelButtonText: '取消',
            }).then(({ value }) => {
                localStorage.setItem('wsurl', value);
                wsUrl.value = value;
                initWebsocket(wsUrl.value);
            })
        }
        const wsUrl = ref(localStorage.getItem('wsurl') || 'ws://127.0.0.1:59411');
        onMounted(() => {
            initWebsocket(wsUrl.value);
        });

        return { connected, wsUrl, editWsUrl }
    }
}
</script>

<style lang="stylus" scoped>
a {
    color: #555;
    text-decoration: underline;
    line-height: 2rem;

    &.connected {
        color: green;
        font-weight: bold;
    }
}
</style>