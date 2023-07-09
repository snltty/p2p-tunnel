<template>
    <div class="vea-dhcp-setting-wrap flex flex-column h-100">
        <div class="inner">
            <div class="head flex">
                <div class="ip-input">
                    <el-input v-model="state.ip1" size="small"></el-input>
                    <span>.</span>
                    <el-input v-model="state.ip2" size="small"></el-input>
                    <span>.</span>
                    <el-input v-model="state.ip3" size="small"></el-input>
                    <span>.0</span>
                </div>
                <span class="split"></span>
                <el-popconfirm title="是否确定保存ip?" @confirm="handleUpdate">
                    <template #reference>
                        <el-button type="primary" size="small" :loading="state.loading">保存</el-button>
                    </template>
                </el-popconfirm>
                <span class="split"></span>
                <span class="split"></span>
                <span class="split"></span>
                <el-button size="small" :loading="state.loading" @click="loadData">刷新列表</el-button>
                <span class="flex-1"></span>
            </div>
            <div class="body flex-1 relative">
                <div>
                    <el-table border stripe :data="state.data" size="small">
                        <el-table-column type="index" width="40" />
                        <el-table-column prop="IP" label="IP" width="120">
                            <template #default="scope">
                                <a href="javascript:;" @click="handleModifyIP(scope.row)">{{scope.row.IP}}</a>
                            </template>
                        </el-table-column>
                        <el-table-column prop="Name" label="节点">
                            <template #default="scope">
                                <span :class="{online:scope.row.OnLine}">{{scope.row.Name}}</span>
                            </template>
                        </el-table-column>
                        <el-table-column prop="LastTime" label="最后使用"></el-table-column>
                        <el-table-column fixed="right" label="op" width="50">
                            <template #default="scope">
                                <el-popconfirm title="删除不可逆，是否确认?" @confirm="handleDel(scope.row)">
                                    <template #reference>
                                        <el-button link size="small">删除</el-button>
                                    </template>
                                </el-popconfirm>
                            </template>
                        </el-table-column>
                    </el-table>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import { computed, onMounted, reactive } from 'vue';
import { getConfig, addNetwork, modifyIP, deleteIP } from '../../../../apis/vea-server'
import { ElMessage } from 'element-plus/lib/components';
import { ElMessageBox } from 'element-plus';
import { injectSignIn } from '../../../../states/signin'
import { shareData } from '../../../../states/shareData'
import plugin from './plugin'
export default {
    components: {},
    setup() {

        const signinState = injectSignIn();
        const hasAccess = computed(() => shareData.serverAccessHas(signinState.RemoteInfo.Access, plugin.access));
        const state = reactive({
            loading: false,
            ip1: 191,
            ip2: 168,
            ip3: 54,
            data: []
        });
        const loadData = () => {
            state.loading = true;
            getConfig().then((res) => {
                state.loading = false;
                [state.ip1, state.ip2, state.ip3] = res.IP.toIpv4Str().split('.');
                state.data = Object.keys(res.Assigned).map(c => {
                    let item = res.Assigned[c];
                    return {
                        IP: item.IP.toIpv4Str(),
                        ip: item.IP,
                        Name: item.Name,
                        LastTime: item.LastTime,
                        OnLine: item.OnLine,
                        connectionid: c
                    }
                });
            }).catch(() => {
                state.loading = false;
            });
        };
        onMounted(() => {
            loadData();
        })

        const handleUpdate = () => {
            state.loading = true;
            addNetwork(`${state.ip1}.${state.ip2}.${state.ip3}.0`.ipv42number()).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('操作成功!');
                    loadData();
                } else {
                    ElMessage.error('操作失败!');
                }
            }).catch(() => {
                state.loading = false;
            });
        }
        const handleModifyIP = (row) => {
            ElMessageBox.prompt('修改ip', '修改ip', {
                inputValue: row.ip & 0xff,
                confirmButtonText: '确定',
                cancelButtonText: '取消',
            }).then(({ value }) => {
                value = +value;
                if (isNaN(value)) {
                    return;
                }
                state.loading = true;
                modifyIP(row.connectionid, value).then((res) => {
                    state.loading = false;
                    if (res) {
                        ElMessage.success('操作成功!');
                        loadData();
                    } else {
                        ElMessage.error('操作失败!');
                    }
                }).catch(() => {
                    state.loading = false;
                });
            })
        }
        const handleDel = (row) => {
            state.loading = true;
            deleteIP(row.connectionid).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('操作成功!');
                    loadData();
                } else {
                    ElMessage.error('操作失败!');
                }
            }).catch(() => {
                state.loading = false;
            });
        }

        return { state, loadData, handleUpdate, handleModifyIP, handleDel, hasAccess }
    }
}
</script>

<style lang="stylus" scoped>
.vea-dhcp-setting-wrap {
    box-sizing: border-box;

    .inner {
        background-color: #fff;
    }

    .head {
        margin-bottom: 1rem;
        line-height: 24px;

        .ip-input {
            line-height: 2.2rem;

            .el-input {
                width: 3.4rem;
            }
        }
    }

    span.online {
        font-weight: bold;
        color: green;
    }
}
</style>