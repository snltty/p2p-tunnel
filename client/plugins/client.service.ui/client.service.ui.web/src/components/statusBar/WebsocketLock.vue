<template>
    <div class="lock-wrap absolute" v-if="lock">
        <div class="inner absolute">
            <p><img src="../../assets/sleep.svg" alt="sleep" width="80"></p>
            <p class="text">需要输入UI密码</p>
            <p class="mt">
                <el-input type="password" show-password v-model="inputValue" />
            </p>
            <p class="mt">
                <el-button @click="validate">验证</el-button>
            </p>
        </div>
    </div>
</template>

<script>
import { computed, ref } from 'vue';
import { injectSignIn } from '../../states/signin'
import { injectWebsocket } from '../../states/websocket'
export default {
    setup() {
        const websocketState = injectWebsocket();
        const signState = injectSignIn();
        const looklookValue = ref(false);
        const lock = computed(() => websocketState.connected && looklookValue.value == false && !!signState.ClientConfig.UIPassword);

        const inputValue = ref('');
        const validate = () => {
            looklookValue.value = inputValue.value == signState.ClientConfig.UIPassword;
        }
        return { signState, inputValue, lock, validate }
    }
}
</script>

<style lang="stylus" scoped>
.lock-wrap {
    background-color: rgba(0, 0, 0, 0.2);
    z-index: 98;

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