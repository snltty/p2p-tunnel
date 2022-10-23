<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-10-23 11:33:46
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
                            <dl v-loading="item.loading">
                                <dt @click="handleClientClick(item)">{{item.Name}}</dt>
                                <dd v-if="item.showUdp" :style="item.udpConnectTypeStyle" class="flex">
                                    <span class="label">Udp</span>
                                    <span>{{item.udpConnectTypeStr}}</span>
                                    <span class="flex-1"></span>
                                    <Signal :value="item.UdpPing"></Signal>
                                </dd>
                                <dd v-if="item.showTcp" :style="item.tcpConnectTypeStyle" class="flex">
                                    <span class="label">Tcp</span>
                                    <span>{{item.tcpConnectTypeStr}}</span>
                                    <span class="flex-1"></span>
                                    <Signal :value="item.TcpPing"></Signal>
                                </dd>
                                <dd class="t-r">
                                    <el-button plain text bg :disabled="item.connectDisabled" size="small" @click="handleConnect(item)">连它</el-button>
                                    <el-button plain text bg :disabled="item.connectDisabled" size="small" @click="handleConnectReverse(item)">连我</el-button>
                                    <el-button plain text bg :loading="item.loading" size="small" @click="handleConnectReset(item)">重启</el-button>
                                </dd>
                            </dl>
                        </div>
                    </el-col>
                </template>
            </el-row>
        </div>
    </div>
    <el-dialog title="试一下发送数据效率" v-model="showTest" center :close-on-click-modal="false" width="50rem">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="10rem">
            <el-form-item label="包数量" prop="Count">
                <el-input v-model="form.Count" />
            </el-form-item>
            <el-form-item label="包大小(KB)" prop="KB">
                <el-input v-model="form.KB" />
            </el-form-item>
            <el-form-item label="结果" prop="">
                {{result}}
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showTest = false">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, reactive, ref, toRefs } from '@vue/reactivity';
import { injectClients } from '../../states/clients'
import { injectRegister } from '../../states/register'
import { sendClientConnect, sendClientConnectReverse, sendClientReset, sendPing } from '../../apis/clients'
import { sendPacketTest } from '../../apis/test'
import Signal from './Signal.vue'
import { onMounted, onUnmounted } from '@vue/runtime-core';
export default {
    name: 'Clients',
    components: { Signal },
    setup () {
        const clientsState = injectClients();
        const registerState = injectRegister();
        const localIp = computed(() => registerState.LocalInfo.LocalIp.split('.').slice(0, 3).join('.'));
        const connectTypeStrs = ['未连接', '已连接-打洞', '已连接-中继'];
        const connectTypeColors = ['color:#333;', 'color:#148727;font-weight:bold;', 'color:#148727;font-weight:bold;'];
        const clients = computed(() => {
            clientsState.clients.forEach(c => {
                c.showUdp = c.UseUdp && registerState.ClientConfig.UseUdp;
                c.showTcp = c.UseTcp && registerState.ClientConfig.UseTcp;

                c.udpConnectType = c.UdpConnected ? c.UdpConnectType : Number(c.UdpConnected);
                c.tcpConnectType = c.TcpConnected ? c.TcpConnectType : Number(c.TcpConnected);

                c.udpConnectTypeStr = connectTypeStrs[c.udpConnectType];
                c.udpConnectTypeStyle = connectTypeColors[c.udpConnectType];

                c.tcpConnectTypeStr = connectTypeStrs[c.tcpConnectType];
                c.tcpConnectTypeStyle = connectTypeColors[c.tcpConnectType];

                c.connectDisabled = false;
                if (c.UseUdp || c.UseTcp) {
                    c.connectDisabled = c.UdpConnected && c.TcpConnected;
                } else if (c.UseUdp) {
                    c.connectDisabled = c.UdpConnected;
                } else if (c.UseTcp) {
                    c.connectDisabled = c.TcpConnected;
                }

                c.online = c.UdpConnected || c.TcpConnected;

                c.loading = c.UdpConnecting || c.TcpConnecting;
            });
            return clientsState.clients;
        });

        const handleConnect = (row) => {
            sendClientConnect(row.Id);
        }
        const handleConnectReverse = (row) => {
            sendClientConnectReverse(row.Id);
        }
        const handleConnectReset = (row) => {
            sendClientReset(row.Id);
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
            showTest: false,
            loading: false,
            Id: 0,
            result: '',
            form: {
                Count: 10000,
                KB: 1
            },
            rules: {
                Count: [
                    { required: true, message: '必填', trigger: 'blur' },
                ],
                KB: [
                    { required: true, message: '必填', trigger: 'blur' }
                ]
            }
        });
        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                sendPacketTest(state.Id, state.form.Count, state.form.KB).then((res) => {
                    state.loading = false;
                    state.result = `${res.Ms} ms、${res.Us} us、${res.Ticks} ticks`;
                }).catch((err) => {
                    state.loading = false;
                });
            });
        }
        const handleClientClick = (row) => {
            state.Id = row.Id;
            state.showTest = true;
        }

        return {
            ...toRefs(state), registerState, handleSubmit, formDom, handleClientClick,
            clients, handleConnect, handleConnectReverse, handleConnectReset, localIp
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
            padding: 0.4rem 1rem;

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