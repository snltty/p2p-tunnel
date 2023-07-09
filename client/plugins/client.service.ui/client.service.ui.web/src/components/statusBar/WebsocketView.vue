<template>
    <div class="ws-wrap absolute" v-if="websocketState.connected == false && looklookValue == false">
        <div class="inner absolute">
            <p><img src="../../assets/sleep.svg" alt="sleep" width="80"></p>
            <p class="text">尚未连接管理接口</p>
            <p class="mt">
                <el-input v-model="websocketState.url" /> <el-button @click="editWsUrl">修改</el-button>
            </p>
            <p class="mt">
                <a href="javascript:;" @click="looklook">不想连接，随便看看？</a>
            </p>
        </div>
    </div>
</template>

<script>
import { injectWebsocket } from '../../states/websocket'
import { initWebsocket } from '../../apis/request'
import { useRoute, useRouter } from 'vue-router'
import { ref } from 'vue'
export default {
    setup() {
        const route = useRoute();
        const router = useRouter();

        const websocketState = injectWebsocket();
        const editWsUrl = () => {
            localStorage.setItem('wsurl', websocketState.url);
            initWebsocket(websocketState.url);
        }
        const looklookValue = ref(false);
        const looklook = () => {
            looklookValue.value = true;
        }
        router.isReady().then(() => {
            if (route.query.port) {
                localStorage.setItem('wsurl', `ws://127.0.0.1:${route.query.port}`);
            }
            websocketState.url = (localStorage.getItem('wsurl') || `ws://127.0.0.1:${route.query.port || 5412}`);
            initWebsocket(websocketState.url);
        });

        return { websocketState, looklookValue, editWsUrl, looklook }
    }
}
</script>

<style lang="stylus" scoped>
.ws-wrap {
    background-color: rgba(0, 0, 0, 0.2);
    z-index: 99;

    .inner {
        background-color: #fff;
        left: 50%;
        top: 50%;
        transform: translateX(-50%) translateY(-50%);
        border: 1px solid #c2c2c2;
        border-radius: 4px;
        text-align: center;
        padding: 4rem;

        .text {
            font-size: 1.6rem;
            color: #333;
        }

        .mt {
            margin-top: 2rem;

            .el-input {
                width: 20rem;
            }

            a {
                text-decoration: underline;
                color: #333;
            }
        }
    }
}
</style>