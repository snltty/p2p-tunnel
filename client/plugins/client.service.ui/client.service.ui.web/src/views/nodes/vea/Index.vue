<template>
    <div class="socks5-wrap">
        <div class="form">
            <el-form ref="formDom" label-width="0">
                <el-form-item>
                    <div class="w-100 t-c">
                        <ConnectButton :loading="state.loading" v-model="state.enable" @handle="handle"></ConnectButton>
                    </div>
                </el-form-item>
                <el-form-item>
                    <div class="w-100 t-c">
                        <el-select v-model="state.targetName" placeholder="选择目标" @change="handleChange">
                            <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
                            </el-option>
                        </el-select>
                    </div>
                </el-form-item>
            </el-form>
        </div>
        <div class="inner">
            <h3 class="title t-c">
                <span>组网列表</span>
                <el-button type="primary" size="small" :loading="state.loading" @click="handleUpdate">刷新列表</el-button>
            </h3>
            <div>
                <el-table size="small" border :data="showClients" style="width: 100%">
                    <el-table-column prop="Name" label="客户端">
                        <template #default="scope">
                            <strong v-if="scope.row.Connected" style="color:green">{{scope.row.Name}}</strong>
                            <span v-else>{{scope.row.Name}}</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="veaIp" label="虚拟ip">
                        <template #default="scope">
                            <p><strong>{{scope.row.veaIp.IP}}</strong></p>
                            <template v-for="(item) in scope.row.veaIp.LanIPs" :key="item">
                                <p style="color:#999">{{item}}</p>
                            </template>

                        </template>
                    </el-table-column>
                    <el-table-column prop="todo" label="操作">
                        <template #default="scope">
                            <el-button size="small" :loading="state.loading" @click="handleResetVea(scope.row)" class="m-r-1">重装其网卡</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
</template>

<script>
import { computed, reactive } from '@vue/reactivity'
import { getConfig, setConfig, getList, reset, update } from '../../../apis/vea'
import { onMounted, onUnmounted } from '@vue/runtime-core'
import { ElMessage } from 'element-plus'
import { injectClients } from '../../../states/clients'
import { websocketState } from '../../../apis/request'
import ConnectButton from '../../../components/ConnectButton.vue'
export default {
    components: { ConnectButton },
    setup() {

        const clientsState = injectClients();
        const targets = computed(() => {
            return clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            });
        });
        const state = reactive({
            loading: false,
            enable: false,
            targetName: '',
            veaClients: {}
        });
        const showClients = computed(() => {
            clientsState.clients.forEach(c => {
                c.veaIp = JSON.parse(JSON.stringify(state.veaClients[c.Id] || { IP: '', LanIPs: [] }));
                c.veaIp.LanIPs = c.veaIp.LanIPs.filter(c => c.length > 0);
            });
            return clientsState.clients;
        });

        const loadConfig = () => {
            getConfig().then((res) => {
                state.enable = res.Enable;
                state.targetName = res.TargetName;
            });
        }



        onMounted(() => {
            handleUpdate();
            loadConfig();
            loadVeaClients();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        });

        const submit = () => {
            state.loading = true;
            getConfig().then((res) => {
                res.targetName = state.targetName;
                res.Enable = state.enable;
                setConfig(res).then(() => {
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
            state.enable = !state.enable;
            submit();
        };
        const handleChange = (name) => {
            if (state.loading) return;
            state.TargetName = TargetName;
            submit();
        }

        const handleResetVea = (row) => {
            state.loading = true;
            reset(row.Id).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('成功');
                } else {
                    ElMessage.error('失败');
                }
            }).catch(() => {
                state.loading = false;
                ElMessage.error('失败');
            });
        }
        const handleUpdate = () => {
            update().then(() => {
                ElMessage.success('已更新');
            })
        }
        let timer = 0;
        const loadVeaClients = () => {
            if (websocketState.connected) {
                getList().then((res) => {
                    state.veaClients = res;
                    timer = setTimeout(loadVeaClients, 1000);
                });
            } else {
                state.veaClients = {};
                timer = setTimeout(loadVeaClients, 1000);
            }
        }

        return {
            targets, state, showClients, handleResetVea, handleUpdate, handle, handleChange
        }
    }
}
</script>

<style lang="stylus" scoped>
.socks5-wrap {
    padding: 5rem 1rem 2rem 1rem;
}

.inner {
    border: 1px solid #ddd;
    padding: 1rem;
    border-radius: 0.4rem;
    margin-bottom: 1rem;
}

@media screen and (max-width: 768px) {
    .el-col {
        margin-top: 0.6rem;
    }
}
</style>