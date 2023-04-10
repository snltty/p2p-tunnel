<template>
    <div class="socks5-wrap">
        <div class="form">
            <el-form label-width="0">
                <el-form-item>
                    <div class="w-100 t-c">
                        <ConnectButton :loading="state.loading" v-model="state.listenEnable" @handle="handle"></ConnectButton>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c">
                        <span>目标</span>：<el-select v-model="state.targetName" placeholder="选择目标" @change="handleChange">
                            <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
                            </el-option>
                        </el-select>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c" style="line-height:1.8rem">
                        <p>代理地址: 127.0.0.1:{{state.listenPort}}</p>
                        <p>自动设置代理有可能失败，可以手动配置系统代理“使用设置脚本”</p>
                        <p>预置pac规则文件地址 <strong>{{state.localtion}}/socks.pac</strong></p>
                        <p>自定义pac规则文件地址 <strong>{{state.localtion}}/socks-custom.pac</strong></p>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { computed, reactive } from '@vue/reactivity'
import { get, set, run } from '../../../apis/socks5'
import { onMounted } from '@vue/runtime-core'
import { injectClients } from '../../../states/clients'
import ConnectButton from '../../../components/ConnectButton.vue'
import plugin from './plugin'
export default {
    plugin: plugin,
    components: { ConnectButton },
    setup() {

        const clientsState = injectClients();
        const targets = computed(() => {
            return [{ Name: '/', label: '服务器' }].concat(clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            }));
        });
        const state = reactive({
            loading: false,
            localtion: window.location.origin,
            listenEnable: false,
            listenPort: 5412,
            targetName: ''
        });
        const loadConfig = () => {
            get().then((res) => {
                state.listenEnable = res.ListenEnable;
                state.listenPort = res.ListenPort;
                state.targetName = res.TargetName;
            });
        }
        onMounted(() => {
            loadConfig();
        })

        const submit = () => {
            state.loading = true;
            get().then((res) => {
                res.TargetName = state.targetName;
                res.ListenEnable = state.listenEnable;
                set(res).then(() => {
                    loadConfig();
                    run().then(() => {
                        state.loading = false;
                    }).catch(() => {
                        state.loading = false;
                    });
                }).catch(() => {
                    state.loading = false;
                });
            }).catch(() => {
                state.loading = false;
            });
        }
        const handle = () => {
            if (state.loading) return;
            state.listenEnable = !state.listenEnable;
            submit();
        };
        const handleChange = (name) => {
            if (state.loading) return;
            state.targetName = name;
            submit();
        }
        return {
            targets, state, handle, handleChange
        }
    }
}
</script>

<style lang="stylus" scoped>
.socks5-wrap {
    padding-top: 5rem;
}
</style>