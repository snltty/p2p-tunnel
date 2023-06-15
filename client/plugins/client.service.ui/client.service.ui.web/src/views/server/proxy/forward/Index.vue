<template>
    <div class="forward-wrap">
        <div class="inner">
            <div class="head flex">
                <el-button type="primary" size="small" @click="handleAddListen">增加长连接端口</el-button>
                <el-button size="small" @click="loadPorts">刷新列表</el-button>
                <span class="flex-1"></span>
            </div>
            <div class="content">
                <el-table :data="state.list" size="small" border>
                    <el-table-column type="expand">
                        <template #default="props">
                            <el-table size="small" :data="props.row.Forwards" border>
                                <el-table-column label="访问" prop="SourceIp">
                                    <template #default="props1">
                                        <span>{{props1.row.sourceText}}</span>
                                    </template>
                                </el-table-column>
                                <el-table-column label="目标" prop="TargetIp">
                                    <template #default="props1">
                                        <span>【本机】{{ props1.row.distText}}</span>
                                    </template>
                                </el-table-column>
                                <el-table-column align="right" width="90">
                                    <template #default="props1">
                                        <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(props.row,props1.row)">
                                            <template #reference>
                                                <el-button link plain type="danger" size="small" v-if="props.row.AliveType == shareData.aliveTypesName.web">删除</el-button>
                                            </template>
                                        </el-popconfirm>
                                        <el-button link plain size="small" @click.stop="handleTestForward(props.row,props1.row)">测试</el-button>
                                    </template>
                                </el-table-column>
                            </el-table>
                        </template>
                    </el-table-column>
                    <el-table-column label="监听类别" prop="AliveType" width="80">
                        <template #default="props">
                            <span>{{shareData.aliveTypes[props.row.AliveType]}}</span>
                        </template>
                    </el-table-column>
                    <el-table-column label="监听端口" prop="Port">
                        <template #default="props">
                            <div class="flex">
                                <span class="flex-1">{{props.row.Domain}}:{{props.row.ServerPort}}</span>
                                <span v-if="props.row.AliveType == shareData.aliveTypesName.tunnel">
                                    <el-switch size="small" @click.stop @change="onListeningChange(props.row,props.row.Forwards[0])" v-model="props.row.Forwards[0].Listening"></el-switch>
                                </span>
                            </div>
                        </template>
                    </el-table-column>
                    <el-table-column label="备注" prop="Desc"></el-table-column>
                    <el-table-column align="right" width="90">
                        <template #default="props">
                            <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(props.row,props.row.Forwards[0])">
                                <template #reference>
                                    <el-button plain link type="danger" size="small">删除</el-button>
                                </template>
                            </el-popconfirm>
                            <el-button plain type="info" link v-if="props.row.AliveType == shareData.aliveTypesName.web" size="small" @click="handleAddForward(props.row)">转发</el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
        <AddForward v-if="state.showAddForward" v-model="state.showAddForward" @success="loadPorts"></AddForward>
        <AddListen v-if="state.showAddListen" v-model="state.showAddListen" @success="loadPorts"></AddListen>
        <StatusMsg v-if="state.showStatusMsg" v-model="state.showStatusMsg" :msgCallback="state.statusMsgCallback"></StatusMsg>
    </div>
</template>

<script>
import { onMounted, provide, reactive, ref, watch } from '@vue/runtime-core';
import { getServerPorts, getServerForwards, startServerForward, stopServerForward, removeServerForward } from '../../../../apis/forward-server'
import { testForward } from '../../../../apis/forward'
import { injectShareData } from '../../../../states/shareData'
import { injectSignIn } from '../../../../states/signin'
import AddForward from './AddForward.vue'
import AddListen from './AddListen.vue'
import StatusMsg from '../../../../components/StatusMsg.vue'
import plugin from './plugin'
import { ElMessageBox } from 'element-plus';
export default {
    plugin: plugin,
    components: { AddForward, AddListen, StatusMsg },
    setup() {

        const shareData = injectShareData();
        const signinState = injectSignIn();
        const state = reactive({
            loading: false,
            list: [],
            showAddForward: false,
            showAddListen: false,
            showStatusMsg: false,
            statusMsgCallback: () => 0
        });
        watch(() => signinState.ServerConfig.Ip, () => {
            loadPorts();
        });

        const expandKeys = ref([]);
        const onExpand = (a, b) => {
            expandKeys.value = b.map(c => c.ServerPort);
        }

        const loadPorts = () => {
            getServerPorts().then((res) => {
                loadForwards(res);
            });
        }
        const loadForwards = (ports) => {
            getServerForwards().then((forwards) => {
                state.list = ports.splice(0, ports.length - 2).map(c => {
                    return {
                        ServerPort: c,
                        Domain: signinState.ServerConfig.Ip,
                        Desc: '短链接',
                        AliveType: shareData.aliveTypesName.web,
                        Forwards: forwards.filter(d => d.AliveType == shareData.aliveTypesName.web && d.ServerPort == c).map(d => {
                            return {
                                Domain: d.Domain,
                                Listening: d.Listening,
                                Desc: d.Desc,
                                LocalIp: d.LocalIp,
                                LocalPort: d.LocalPort,
                                sourceText: `${d.Domain}:${d.ServerPort}`,
                                distText: `${d.LocalIp}:${d.LocalPort}`
                            }
                        })
                    }
                }).concat(forwards.filter(c => c.AliveType == shareData.aliveTypesName.tunnel).map(d => {
                    return {
                        ServerPort: d.ServerPort,
                        Domain: signinState.ServerConfig.Ip,
                        Desc: d.Desc || '长连接',
                        AliveType: shareData.aliveTypesName.tunnel,
                        Listening: d.Listening,
                        Forwards: [
                            {
                                Domain: d.Domain,
                                Listening: d.Listening,
                                Desc: d.Desc,
                                LocalIp: d.LocalIp,
                                LocalPort: d.LocalPort,
                                sourceText: `${signinState.ServerConfig.Ip}:${d.ServerPort}`,
                                distText: `${d.LocalIp}:${d.LocalPort}`
                            }
                        ]
                    }
                }));
            })
        }

        const onListeningChange = (listen, forward) => {
            let func = !forward.Listening ? stopServerForward : startServerForward;
            state.loading = true;
            func({
                AliveType: listen.AliveType,
                Domain: forward.Domain,
                ServerPort: listen.ServerPort,
                LocalIp: forward.LocalIp,
                LocalPort: forward.LocalPort
            }).then((res) => {
                state.loading = false;
                if (res) {
                    loadPorts();
                }
            }).catch(() => {
                state.loading = false;
            })
        }
        const handleRemoveListen = (listen, forward) => {
            state.loading = true;
            removeServerForward({
                AliveType: listen.AliveType,
                Domain: forward.Domain,
                ServerPort: listen.ServerPort,
                LocalIp: forward.LocalIp,
                LocalPort: forward.LocalPort
            }).then((res) => {
                state.loading = false;
                if (res) {
                    loadPorts();
                }
            }).catch(() => {
                state.loading = false;
            })
        }

        const addForwardData = ref({ AliveType: shareData.aliveTypesName.web, ServerPort: 0 });
        provide('add-forward-data', addForwardData);
        const handleAddForward = (row) => {
            addForwardData.value.ServerPort = row.ServerPort;
            state.showAddForward = true;
        }

        const addListenData = ref({ AliveType: shareData.aliveTypesName.tunnel });
        provide('add-listen-data', addListenData);
        const handleAddListen = () => {
            state.showAddListen = true;
        }
        const handleTestForward = (listen, forward) => {
            let host = listen.AliveType == shareData.aliveTypesName.tunnel ? signinState.ServerConfig.Ip : forward.Domain;
            let port = listen.ServerPort;
            ElMessageBox.prompt('不带http://，带端口', '测试', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputValue: `${host}:${port}`
            }).then(({ value }) => {
                let arr = value.split(':');
                state.statusMsgCallback = testForward(arr[0], +arr[1]);
                state.showStatusMsg = true;
            }).catch(() => {
            });
        }
        onMounted(() => {
            loadPorts();
        });

        return {
            state, shareData, loadPorts, onExpand, expandKeys,
            handleRemoveListen, handleAddForward, handleAddListen, onListeningChange, handleTestForward
        }
    }
}
</script>

<style lang="stylus">
.forward-wrap {
    .el-collapse-item__header, .el-collapse-item__content, .el-collapse-item__wrap {
        border-right: 0;
        border-left: 0;
    }

    .el-collapse-item__content {
        padding: 0;
    }
}
</style>
<style lang="stylus" scoped>
@media screen and (max-width: 500px) {
    .el-col-24 {
        max-width: 100%;
        flex: 0 0 100%;
    }
}

.forward-wrap {
    padding: 2rem;

    .inner {
        border: 1px solid var(--main-border-color);
        // padding: 1rem;
        border-radius: 0.4rem;
        background-color: #Fff;
    }

    .head {
        padding: 1rem;
        border-bottom: 1px solid var(--main-border-color);
    }

    .content {
        padding: 1rem;

        .item {
            padding: 1rem 0.6rem;

            dl {
                border: 1px solid var(--main-border-color);
                border-radius: 0.4rem;

                dt {
                    border-bottom: 1px solid var(--main-border-color);
                    padding: 1rem;
                    font-size: 1.4rem;
                    font-weight: 600;
                    color: #555;
                    line-height: 2.4rem;
                }

                dd {
                    padding: 0.4rem 1rem;

                    &:nth-child(2) {
                        padding: 1rem;
                        background-color: #fafafa;
                    }

                    &.forwards {
                        padding: 0;

                        li {
                            border-bottom: 1px solid #eee;
                            padding: 1rem;

                            &:last-child {
                                border: 0;
                            }
                        }
                    }
                }
            }
        }
    }

    .alert {
        margin-top: 1rem;
    }
}
</style>