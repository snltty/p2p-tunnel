<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-30 23:05:54
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
                            <dl v-loading="item.UdpConnecting || item.TcpConnecting">
                                <dt @click="handleClientClick(item)">{{item.Name}}</dt>
                                <dd v-if="item.Udp && registerState.ClientConfig.UseUdp" :style="item.udpConnectTypeStyle" class="flex">
                                    <span class="label">Udp</span>
                                    <el-icon>
                                        <connection />
                                    </el-icon>
                                    <span>{{item.udpConnectTypeStr}}</span>
                                </dd>
                                <dd v-if="item.Tcp  && registerState.ClientConfig.UseTcp" :style="item.tcpConnectTypeStyle" class="flex">
                                    <span class="label">Tcp</span>
                                    <el-icon>
                                        <connection />
                                    </el-icon>
                                    <span>{{item.tcpConnectTypeStr}}</span>
                                </dd>
                                <dd class="t-r">
                                    <el-button plain text bg :disabled="item.connectDisabled" size="small" @click="handleConnect(item)">连它</el-button>
                                    <el-button plain text bg :disabled="item.connectDisabled" size="small" @click="handleConnectReverse(item)">连我</el-button>
                                    <el-button plain text bg :loading="item.UdpConnecting || item.TcpConnecting" size="small" @click="handleConnectReset(item)">重启</el-button>
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
import { sendClientConnect, sendClientConnectReverse, sendClientReset } from '../../apis/clients'
import { sendPacketTest } from '../../apis/test'
export default {
    name: 'Clients',
    components: {},
    setup () {
        const clientsState = injectClients();
        const registerState = injectRegister();
        const localIp = computed(() => registerState.LocalInfo.LocalIp.split('.').slice(0, 3).join('.'));

        const handleConnect = (row) => {
            sendClientConnect(row.Id);
        }
        const handleConnectReverse = (row) => {
            sendClientConnectReverse(row.Id);
        }
        const handleConnectReset = (row) => {
            sendClientReset(row.Id);
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
            ...toRefs(clientsState), handleConnect, handleConnectReverse, handleConnectReset, localIp
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