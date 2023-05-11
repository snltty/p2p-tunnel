<template>
    <div class="proxy-wrap">
        <div class="form">
            <el-form label-width="0">
                <el-form-item>
                    <div class="w-100 t-c">
                        <ConnectButton :loading="state.loading" v-model="state.ListenEnable" @handle="handle"></ConnectButton>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c">
                        <span>目标</span>：<el-select v-model="state.TargetConnectionId" placeholder="选择目标" @change="handleChange">
                            <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.id">
                            </el-option>
                        </el-select>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c" style="line-height:1.8rem">
                        <p>代理地址: {{state.ProxyIp}}:{{state.ListenPort}}</p>
                        <p>自动设置代理有可能失败，可以手动配置系统代理“使用设置脚本”</p>
                        <p>预置pac规则文件地址 <strong>http://{{state.ProxyIp}}:{{state.port}}/proxy.pac</strong></p>
                        <p>自定义pac规则文件地址 <strong>http://{{state.ProxyIp}}:{{state.port}}/proxy-custom.pac</strong></p>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { computed, reactive } from '@vue/reactivity'
import { getConfigure, saveConfigure } from '../../../apis/configure'
import { update } from '../../../apis/httpproxy'
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
            return [{ id: 0, label: '服务器' }].concat(clientsState.clients.map(c => {
                return { id: c.ConnectionId, label: c.Name }
            }));
        });
        const state = reactive({
            loading: false,

            ListenPort: 5414,
            TargetConnectionId: 0,
            ListenEnable: false,
            ProxyIp: '127.0.0.1',

            port: window.location.port,
        });
        const loadConfig = () => {
            getConfigure(plugin.config).then((res) => {
                const json = new Function(`return ${res}`)();
                state.ListenPort = json.ListenPort;
                state.TargetConnectionId = json.TargetConnectionId;
                state.ListenEnable = json.ListenEnable;
                state.ProxyIp = json.ProxyIp;

            });
        }
        onMounted(() => {
            loadConfig();
        });

        const submit = () => {
            state.loading = true;
            getConfigure(plugin.config).then((res) => {
                const json = new Function(`return ${res}`)();
                json.TargetConnectionId = state.TargetConnectionId;
                json.ListenEnable = state.ListenEnable;
                saveConfigure(plugin.config, JSON.stringify(json)).then(() => {
                    update().then(() => {
                        loadConfig();
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
            state.ListenEnable = !state.ListenEnable;
            submit();
        };
        const handleChange = (id) => {
            if (state.loading) return;
            state.TargetConnectionId = id;
            submit();
        }

        return {
            targets, state, handle, handleChange
        }
    }
}
</script>

<style lang="stylus" scoped>
.proxy-wrap {
    padding: 4rem;

    .form {
        padding-top: 3rem;
        background-color: #fff;
        border-radius: 4px;
        border: 1px solid #ddd;
        box-shadow: 0 0 8px 1px rgba(0, 0, 0, 0.05);
    }
}
</style>