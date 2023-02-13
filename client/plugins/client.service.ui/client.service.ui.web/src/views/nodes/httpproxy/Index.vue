<template>
    <div class="proxy-wrap">
        <div class="form">
            <el-form label-width="0">
                <el-form-item>
                    <div class="w-100 t-c">
                        <ConnectButton :loading="state.loading" v-model="state.listening" @handle="handle"></ConnectButton>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c">
                        <span>目标</span>：<el-select v-model="state.name" placeholder="选择目标" @change="handleChange">
                            <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
                            </el-option>
                        </el-select>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c" style="line-height:1.8rem">
                        <p>代理地址: 127.0.0.1:{{state.port}}</p>
                        <p>自动设置代理有可能失败，可以手动配置系统代理“使用设置脚本”</p>
                        <p>预置pac规则文件地址 <strong>{{state.localtion}}/proxy.pac</strong></p>
                        <p>自定义pac规则文件地址 <strong>{{state.localtion}}/proxy-custom.pac</strong></p>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { computed, reactive } from '@vue/reactivity'
import { getListProxy, addListen } from '../../../apis/tcp-forward'
import { onMounted } from '@vue/runtime-core'
import { injectClients } from '../../../states/clients'
import ConnectButton from '../../../components/ConnectButton.vue'
export default {
    components: { ConnectButton },
    setup() {

        const clientsState = injectClients();
        const targets = computed(() => {
            return [{ Name: '', label: '服务器' }].concat(clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            }));
        });
        const state = reactive({
            loading: false,

            port: 5412,
            name: '',
            listening: false,

            localtion: window.location.origin,
        });
        const loadConfig = () => {
            getListProxy().then((res) => {
                state.port = res.Port;
                state.name = res.Name;
                state.listening = res.Listening;
            });
        }
        onMounted(() => {
            loadConfig();
        });

        const submit = () => {
            state.loading = true;
            getListProxy().then((res) => {
                res.Name = state.name;
                res.Listening = state.listening;
                addListen(res).then(() => {
                    state.loading = false;
                    loadConfig();
                }).catch(() => {
                    state.loading = false;
                });
            }).catch(() => {
                state.loading = false;
            });
        }
        const handle = () => {
            if (state.loading) return;
            state.listening = !state.listening;
            submit();
        };
        const handleChange = (name) => {
            if (state.loading) return;
            state.name = name;
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
    padding-top: 5rem;
}
</style>