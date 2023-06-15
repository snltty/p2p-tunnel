<template>
    <div class="socks5-wrap">
        <div class="form">
            <el-form ref="formDom" label-width="0">
                <el-form-item>
                    <div class="w-100 t-c">
                        <ConnectButton :loading="state.loading" v-model="state.ListenEnable" @handle="handle"></ConnectButton>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c">
                        开启后会安装一个虚拟网卡，分配一个虚拟ip
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import { getConfig, setConfig, runVea } from '../../../../apis/vea'
import { onMounted } from '@vue/runtime-core'
import ConnectButton from '../../../../components/ConnectButton.vue'
import plugin from './plugin'
import { ElMessage } from 'element-plus'
export default {
    plugin: plugin,
    components: { ConnectButton },
    setup() {

        const state = reactive({
            loading: false,
            ListenEnable: false
        });

        const loadConfig = () => {
            getConfig().then((res) => {
                state.ListenEnable = res.ListenEnable;
            });
        }

        onMounted(() => {
            loadConfig();
        });

        const submit = () => {
            state.loading = true;
            getConfig().then((res) => {
                res.ListenEnable = state.ListenEnable;
                setConfig(res).then(() => {
                    loadConfig();
                    runVea().then((res1) => {
                        state.loading = false;
                        if (res1 == false) {
                            ElMessage.error('失败,具体信息看日志');
                            state.ListenEnable = false;
                            res.ListenEnable = state.ListenEnable;
                            setConfig(res);
                        }
                    }).catch(() => {
                        state.ListenEnable = false;
                        res.ListenEnable = state.ListenEnable;
                        setConfig(res);
                        state.loading = false;
                    })
                }).catch(() => {
                    state.loading = false;
                });
            }).catch(() => {
                state.loading = false;
            });
        }
        const handle = () => {
            if (state.loading) return;
            state.ListenEnable = !state.ListenEnable;
            submit();
        };
        return {
            state, handle
        }
    }
}
</script>

<style lang="stylus" scoped>
.socks5-wrap {
    padding: 4rem;

    .form {
        padding-top: 3rem;
        background-color: #fff;
        border-radius: 4px;
        border: 1px solid #ddd;
        box-shadow: 0 0 8px 1px rgba(0, 0, 0, 0.05);
    }
}

@media screen and (max-width: 768px) {
    .el-col {
        margin-top: 0.6rem;
    }
}
</style>