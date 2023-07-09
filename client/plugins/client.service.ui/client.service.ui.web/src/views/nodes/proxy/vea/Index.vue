<template>
    <div class="socks5-wrap">
        <el-tabs type="border-card">
            <el-tab-pane label="主页">
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
                        <div class="setting">

                        </div>
                    </el-form>
                </div>
            </el-tab-pane>
            <el-tab-pane label="选项配置">
                <Setting></Setting>
            </el-tab-pane>
            <el-tab-pane label="IP分配管理">
                <IPAssign v-if="hasAccess"></IPAssign>
                <el-empty v-else description="没有此项权限" />
            </el-tab-pane>
        </el-tabs>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity'
import { getConfigure, saveConfigure } from '../../../../apis/configure'
import { runVea } from '../../../../apis/vea'
import { computed, onMounted } from '@vue/runtime-core'
import ConnectButton from '../../../../components/ConnectButton.vue'
import plugin from './plugin'
import Setting from './Setting1.vue'
import IPAssign from './IPAssign.vue'
import { ElMessage } from 'element-plus'
import { injectSignIn } from '../../../../states/signin'
import { shareData } from '../../../../states/shareData'
export default {
    plugin: plugin,
    components: { ConnectButton, Setting, IPAssign },
    setup() {

        const activeNames = ref(['1']);
        const signinState = injectSignIn();
        const hasAccess = computed(() => shareData.serverAccessHas(signinState.RemoteInfo.Access, plugin.access));
        const state = reactive({
            loading: false,
            ListenEnable: false
        });

        const loadConfig = () => {
            getConfigure(plugin.config).then((res) => {
                state.ListenEnable = res.ListenEnable;
            });
        }

        onMounted(() => {
            loadConfig();
        });

        const submit = () => {
            state.loading = true;
            getConfigure(plugin.config).then((res) => {
                res.ListenEnable = state.ListenEnable;
                saveConfigure(plugin.config, JSON.stringify(res)).then(() => {
                    loadConfig();
                    runVea().then((res1) => {
                        state.loading = false;
                        if (res1 == false) {
                            ElMessage.error('失败,具体信息看日志');
                            state.ListenEnable = false;
                            res.ListenEnable = state.ListenEnable;
                            saveConfigure(plugin.config, JSON.stringify(res));
                        }
                    }).catch(() => {
                        state.ListenEnable = false;
                        res.ListenEnable = state.ListenEnable;
                        saveConfigure(plugin.config, JSON.stringify(res));
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
            state, handle, activeNames, hasAccess
        }
    }
}
</script>

<style lang="stylus" scoped>
.socks5-wrap {
    padding: 2rem;

    .el-collapse-item__content {
        padding-bottom: 0;
    }

    .form {
        padding-top: 3rem;
    }

    .setting {
        padding: 0 2rem 2rem 2rem;
    }
}

.el-form-item:last-child {
    margin-bottom: 0;
}

@media screen and (max-width: 768px) {
    .el-col {
        margin-top: 0.6rem;
    }
}
</style>