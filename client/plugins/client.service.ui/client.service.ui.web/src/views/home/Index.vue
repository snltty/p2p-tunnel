<template>
    <div class="home">
        <div class="connection">
            <el-form label-width="0">
                <el-form-item>
                    <div class="w-100 t-c">
                        <ConnectButton :loading="loading" v-model="connected" @handle="handleConnect"></ConnectButton>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c">
                        <ServerLine ref="serverLineDom" @handle="handleSelectServer"></ServerLine>
                    </div>
                </el-form-item>
                <template v-for="(item,index) in infos" :key="index">
                    <el-form-item>
                        <component :is="item" />
                    </el-form-item>
                </template>
            </el-form>
        </div>
        <div class="servers">
            <Servers v-if="state.showServers" v-model="state.showServers" @success="handleSelectServerSuccess"></Servers>
        </div>
    </div>
</template>

<script>
import { computed, reactive, ref, shallowRef } from '@vue/reactivity'
import { injectSignIn } from '../../states/signin'
import { sendSignInMsg, sendExit } from '../../apis/signin'
import ConnectButton from '../../components/ConnectButton.vue'
import ServerLine from './ServerLine.vue'
import Servers from './Servers.vue'
import { ElMessageBox } from 'element-plus/lib/components'
export default {
    name: 'Home',
    components: { ConnectButton, ServerLine, Servers },
    setup() {


        const files = require.context('../', true, /Info\.vue/);
        const infos = files.keys().map(c => files(c).default);

        const signinState = injectSignIn();
        const state = reactive({
            showServers: false,
        });

        const loading = computed(() => signinState.LocalInfo.IsConnecting);
        const connected = computed(() => signinState.LocalInfo.Connected);
        const handleConnect = () => {
            if (loading.value) {
                ElMessageBox.confirm('正在连接，是否确定操作', '提示').then(() => {
                    sendExit();
                }).catch(() => { })
            } else if (connected.value) {
                sendExit();
            } else {
                sendSignInMsg().then((res) => {
                }).catch((msg) => {
                    ElMessage.error(msg);
                });
            }
        }

        const serverLineDom = ref(null);
        const handleSelectServer = () => {
            state.showServers = true;
        }
        const handleSelectServerSuccess = (_state) => {
            if (_state) {
                sendExit();
            }
            serverLineDom.value.update();
        }


        return {
            infos, signinState, state,
            loading, connected, handleConnect,
            serverLineDom, handleSelectServer, handleSelectServerSuccess
        }

    }
}
</script>
<style lang="stylus" scoped>
.connection {
    padding-top: 5rem;
}
</style>