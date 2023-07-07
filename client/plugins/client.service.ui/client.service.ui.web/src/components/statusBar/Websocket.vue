<template>
    <div>
        <a href="javascript:;" @click="editWsUrl" :class="{connected:websocketState.connected}">{{websocketState.url}}</a>
    </div>
</template>

<script>
import { injectWebsocket } from '../../states/websocket'
import { initWebsocket } from '../../apis/request'
import { ElMessageBox } from 'element-plus'
export default {
    components: {},
    setup() {
        const websocketState = injectWebsocket();

        const editWsUrl = () => {
            ElMessageBox.prompt('修改连接地址', '修改连接地址', {
                inputValue: websocketState.url,
                confirmButtonText: '确定',
                cancelButtonText: '取消',
            }).then(({ value }) => {
                localStorage.setItem('wsurl', value);
                websocketState.url = value;
                initWebsocket(websocketState.url);
            })
        }

        return { websocketState, editWsUrl }
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