<template>
    <div class="wrap">
        <div class="content">
            <el-row v-if="clients.length > 0">
                <template v-for="(item,index) in clients" :key="index">
                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                        <div class="item">
                            <dl v-loading="item.Connecting">
                                <dt>{{item.name}}</dt>
                                <dd :style="item.connectTypeStyle" :title="item.IPAddress" class="flex line" @click="handleShowDelay(item)">
                                    <span class="label">{{item.serverType}}</span>
                                    <span>{{item.connectTypeStr}}</span>
                                    <span class="flex-1"></span>
                                    <Signal :value="item.Ping"></Signal>
                                </dd>
                                <dd class="t-c plugins flex">
                                    <template v-for="(item1,index) in components" :key="index">
                                        <div class="item">
                                            <component :is="item1" :params="item"></component>
                                        </div>
                                    </template>
                                </dd>
                                <dd class="t-r btns">
                                    <el-button plain text bg size="small" @click="handleConnect(item)">连它</el-button>
                                    <el-button plain text bg size="small" @click="handleConnectReverse(item)">连我</el-button>
                                    <el-button plain text bg size="small" @click="handleConnectReset(item)">重启</el-button>
                                    <el-button plain text bg size="small" v-if="item.Connected" @click="handleConnectOffline(item)">断开</el-button>
                                </dd>
                            </dl>
                        </div>
                    </el-col>
                </template>
            </el-row>
            <el-empty v-else description="空的，请确保各节点使用了同一分组编号，同一服务器" />
        </div>
        <RelayView v-if="state.showDelay" v-model="state.showDelay" @success="handleOnRelay"></RelayView>
    </div>
</template>

<script>
import { computed, reactive } from '@vue/reactivity';
import { injectClients } from '../../../states/clients'
import { injectSignIn } from '../../../states/signin'
import { injectShareData } from '../../../states/shareData'
import { sendClientConnect, sendClientConnectReverse, sendClientReset, sendClientOffline, sendPing, setRelay } from '../../../apis/clients'
import Signal from '../../../components/Signal.vue'
import RelayView from './RelayView.vue'
import { onMounted, onUnmounted, provide } from '@vue/runtime-core';
import { ElMessageBox } from 'element-plus'
import { injectServices, accessService } from '../../../states/services';
import plugin from './plugin'
export default {
    plugin: plugin,
    components: { Signal, RelayView },
    setup() {

        const servicesState = injectServices();
        const files = require.context('../../', true, /Badge\.vue/);
        const components = files.keys().map(c => files(c).default).filter(c => accessService(c.plugin.service, servicesState));

        const clientsState = injectClients();
        const signinState = injectSignIn();
        const shareDataState = injectShareData();
        const connectTypeColors = {
            0: 'color:#333;',
            1: 'color:#148727;font-weight:bold;',
            2: 'color:#148727;font-weight:bold;',
            4: 'color:#148727;font-weight:bold;',
        };
        const clients = computed(() => {
            clientsState.clients.forEach(c => {
                c.connectTypeStr = shareDataState.clientConnectTypes[c.ConnectType];
                c.connectTypeStyle = connectTypeColors[c.ConnectType];
                c.serverType = shareDataState.serverTypes[c.ServerType];
                c.name = `${c.Name}`
            });
            return clientsState.clients;
        });

        const handleConnect = (row) => {
            if (row.Connected) {
                ElMessageBox.confirm('已连接，是否确定重新连接', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning',
                }).then(() => {
                    sendClientConnect(row.ConnectionId);
                }).catch(() => {

                });
            } else {
                sendClientConnect(row.ConnectionId);
            }
        }
        const handleConnectReverse = (row) => {
            if (row.Connected) {
                ElMessageBox.confirm('已连接，是否确定重新连接', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning',
                }).then(() => {
                    sendClientConnectReverse(row.ConnectionId);
                }).catch(() => {

                });
            } else {
                sendClientConnectReverse(row.ConnectionId);
            }
        }
        const handleConnectReset = (row) => {
            ElMessageBox.confirm('确定重启它吗', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                sendClientReset(row.ConnectionId);
            }).catch(() => {

            });
        }
        const handleConnectOffline = (row) => {
            ElMessageBox.confirm('确定断开连接吗', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                sendClientOffline(row.ConnectionId);
            }).catch(() => {
            });
        }


        let pingTimer = 0;
        onMounted(() => {
            handlePing();
        });
        onUnmounted(() => {
            clearTimeout(pingTimer);
        })
        const handlePing = () => {
            sendPing().then(() => {
                pingTimer = setTimeout(handlePing, 1000);
            }).catch(() => {
                pingTimer = setTimeout(handlePing, 1000);
            })
        }

        const state = reactive({
            showDelay: false,
            toId: 0
        });
        provide('share-data', state);
        const handleShowDelay = (item) => {
            state.toId = item.ConnectionId;
            state.showDelay = true;
        }
        const handleOnRelay = (relayPath) => {
            setRelay(relayPath);
        }


        return {
            components, signinState, clients, handleConnect, handleConnectReverse, handleConnectReset, handleConnectOffline,
            state, handleShowDelay, handleOnRelay
        }

    }
}
</script>
<style lang="stylus" scoped>
.content {
    padding: 1rem;

    .item {
        padding: 1rem 0.6rem;
    }

    dl {
        border: 1px solid #ddd;
        border-radius: 0.4rem;

        dt {
            border-bottom: 1px solid #eee;
            padding: 1rem;
            font-size: 1.4rem;
            font-weight: 600;
            color: #555;
        }

        dd {
            cursor: pointer;
            padding: 0.4rem 1rem;

            &:hover {
                text-decoration: underline;
            }

            &.plugins {
                .item {
                    padding: 0.6rem;
                }

                &:hover {
                    text-decoration: none;
                }

                a {
                    color: #666;
                }

                a:hover {
                    text-decoration: underline;
                }
            }

            &.line {
                padding-top: 1rem;
            }

            &.btns {
                padding-bottom: 1rem;
            }

            .label {
                width: 4rem;
            }
        }
    }
}

@media screen and (max-width: 500px) {
    .el-col-24 {
        max-width: 100%;
        flex: 0 0 100%;
    }
}

@media screen and (max-width: 450px) {
    .content {
        padding-top: 0;

        .item {
            padding: 1rem 0.6rem;
        }
    }
}
</style>