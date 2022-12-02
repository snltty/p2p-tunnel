<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-12-02 10:36:13
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Clients.vue
-->
<template>
    <div class="wrap">
        <h3 class="title t-c">已注册的客户端列表</h3>
        <div class="content">
            <el-row>
                <template v-for="(item,index) in clients" :key="index">
                    <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                        <div class="item">
                            <dl v-loading="item.Connecting">
                                <dt>{{item.Name}}</dt>
                                <dd :style="item.connectTypeStyle" :title="item.IPAddress" class="flex" @click="handleShowDelay(item)">
                                    <span class="label">{{item.serverType}}</span>
                                    <span>{{item.connectTypeStr}}</span>
                                    <span class="flex-1"></span>
                                    <Signal :value="item.Ping"></Signal>
                                </dd>
                                <dd class="t-r">
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
        </div>
        <RelayView v-if="state.showDelay" v-model="state.showDelay" @success="handleOnRelay"></RelayView>
    </div>
</template>

<script>
import { computed, reactive } from '@vue/reactivity';
import { injectClients } from '../../states/clients'
import { injectRegister } from '../../states/register'
import { injectShareData } from '../../states/shareData'
import { sendClientConnect, sendClientConnectReverse, sendClientReset, sendClientOffline, sendPing, setRelay } from '../../apis/clients'
import Signal from './Signal.vue'
import RelayView from './RelayView.vue'
import { onMounted, onUnmounted, provide } from '@vue/runtime-core';
import { ElMessageBox } from 'element-plus'
export default {
    name: 'Clients',
    components: { Signal, RelayView },
    setup () {
        const clientsState = injectClients();
        const registerState = injectRegister();
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
                    sendClientConnect(row.Id);
                }).catch(() => {

                });
            } else {
                sendClientConnect(row.Id);
            }
        }
        const handleConnectReverse = (row) => {
            if (row.Connected) {
                ElMessageBox.confirm('已连接，是否确定重新连接', '提示', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning',
                }).then(() => {
                    sendClientConnectReverse(row.Id);
                }).catch(() => {

                });
            } else {
                sendClientConnectReverse(row.Id);
            }
        }
        const handleConnectReset = (row) => {
            ElMessageBox.confirm('确定重启它吗', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                sendClientReset(row.Id);
            }).catch(() => {

            });
        }
        const handleConnectOffline = (row) => {
            ElMessageBox.confirm('确定断开连接吗', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning',
            }).then(() => {
                sendClientOffline(row.Id);
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
            state.toId = item.Id;
            state.showDelay = true;
        }
        const handleOnRelay = (relayPath) => {
            setRelay(relayPath);
        }


        return {
            registerState, clients, handleConnect, handleConnectReverse, handleConnectReset, handleConnectOffline,
            state, handleShowDelay, handleOnRelay
        }

    }
}
</script>
<style lang="stylus" scoped>
.wrap
    border: 1px solid #eee;
    border-radius: 0.4rem;
    padding: 2rem;

.content
    padding-top: 1rem;

    .item
        padding: 1rem 0.6rem;

    dl
        border: 1px solid #eee;
        border-radius: 0.4rem;

        dt
            border-bottom: 1px solid #eee;
            padding: 1rem;
            font-size: 1.4rem;
            font-weight: 600;
            color: #555;

        dd
            cursor: pointer;
            padding: 0.4rem 1rem;

            &:hover
                text-decoration: underline;

            &:nth-child(2)
                padding-top: 1rem;

            &:last-child
                padding-bottom: 1rem;

            .label
                width: 4rem;

@media screen and (max-width: 500px)
    .el-col-24
        max-width: 100%;
        flex: 0 0 100%;

@media screen and (max-width: 450px)
    .wrap
        padding: 2rem 0.6rem 0.6rem 0.6rem;

    .content
        padding-top: 0;

        .item
            padding: 1rem 0.6rem;
</style>