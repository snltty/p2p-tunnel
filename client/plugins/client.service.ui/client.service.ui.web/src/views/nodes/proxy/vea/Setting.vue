<template>
    <div class="socks5-wrap">
        <div class="inner">
            <div class="form">
                <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="监听端口" prop="ListenPort">
                                        <el-input size="default" v-model="state.form.ListenPort">
                                            <template #append>
                                                <el-tooltip class="box-item" effect="dark" content="监听端口，空闲端口即可" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                        </el-input>
                                    </el-form-item>
                                </el-col>
                                <high-config>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="bufsize" prop="BufferSize">
                                            <el-select size="default" v-model="state.form.BufferSize" placeholder="选择合适的buff">
                                                <el-option v-for="(item,index) in shareData.bufferSizes" :key="index" :label="item" :value="index"></el-option>
                                            </el-select>
                                        </el-form-item>
                                    </el-col>
                                </high-config>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="允许访问" prop="ConnectEnable">
                                        <el-checkbox v-model="state.form.ConnectEnable">开启
                                            <el-tooltip class="box-item" effect="dark" content="作为被访问端时，是否允许访问，不允许则只有权限时可访问" placement="top">
                                                <el-icon>
                                                    <Warning />
                                                </el-icon>
                                            </el-tooltip>
                                        </el-checkbox>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="本机IP" prop="IP">
                                        <el-input size="default" v-model="state.form.IP">
                                            <template #append>
                                                <el-tooltip class="box-item" effect="dark" content="当前节点的虚拟网卡ip，各个节点之间设置不一样的ip，相同网段即可" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                        </el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <high-config>
                        <el-form-item label-width="0">
                            <div class="w-100">
                                <el-row :gutter="10">
                                    <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="开启组播" prop="BroadcastEnable">
                                            <el-checkbox v-model="state.form.BroadcastEnable">开启
                                                <el-tooltip class="box-item" effect="dark" content="是否将组播消息发送到对端" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </el-checkbox>
                                        </el-form-item>
                                    </el-col>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="组播绑定" prop="BroadcastBind">
                                            <el-input size="default" v-model="state.form.BroadcastBind" placeholder="udp组播绑定端点发送，当为0.0.0.0时，组播消息将发送到127.0.0.1">
                                                <template #append>
                                                    <el-tooltip class="box-item" effect="dark" content="udp组播绑定端点发送，当为0.0.0.0时，组播消息将发送到127.0.0.1" placement="top">
                                                        <el-icon>
                                                            <Warning />
                                                        </el-icon>
                                                    </el-tooltip>
                                                </template>
                                            </el-input>
                                        </el-form-item>
                                    </el-col>
                                </el-row>
                            </div>
                        </el-form-item>
                        <el-form-item label-width="0">
                            <div class="w-100">
                                <el-row :gutter="10">
                                    <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
                                        <el-form-item label="组播列表" prop="BroadcastList">
                                            <el-input type="textarea" size="default" v-model="state.form.BroadcastList" resize="none" :autosize="{minRows:4,maxRows:6}" placeholder="允许哪些组播ip，为空则允许所有，多条使用英文逗号间隔或者换行"></el-input>
                                        </el-form-item>
                                    </el-col>
                                </el-row>
                            </div>
                        </el-form-item>
                    </high-config>
                    <el-form-item label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
                                    <el-form-item label="局域网段" prop="LanIPs">
                                        <el-input type="textarea" size="default" v-model="state.form.LanIPs" resize="none" :autosize="{minRows:4,maxRows:6}" placeholder="当前节点的局域网段，多条使用英文逗号间隔或者换行"></el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label-width="0">
                        <div class="w-100">
                            <p>各节点之间，网段不可重复，其它节点可访问本节点所在局域网的其它设备</p>
                            <p style="line-height:2rem">支持掩码，如 192.168.1.100/29，则 192.168.1.97 - 192.168.1.102可用，掩码16+，也就是至多两段(192.168.0.0)，而不能(192.0.0.0)，</p>
                            <p style="line-height:2rem;margin-top:.6rem">
                                <font color="red">局域网段将会添加到其它节点的路由表中，因此，本节点设置的局域网段，不能是其它设备的局域网网段，否则将会产生不可预知的问题</font>
                            </p>
                        </div>
                    </el-form-item>
                </el-form>
            </div>
        </div>
    </div>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity'
import { getConfigure, saveConfigure } from '../../../../apis/configure'
import { onMounted } from '@vue/runtime-core'
import { injectClients } from '../../../../states/clients'
import { shareData } from '../../../../states/shareData'
import plugin from './plugin'
export default {
    plugin: plugin,
    components: {},
    setup() {

        const clientsState = injectClients();
        const targets = computed(() => {
            return clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            });
        });
        const state = reactive({
            configInfo: {},
            form: {
                IP: '',
                LanIPs: '',

                ListenPort: 5415,
                BufferSize: 3,
                ConnectEnable: false,
                BroadcastEnable: false,
                BroadcastBind: '0.0.0.0',
                BroadcastList: '',
            },
            rules: {
                ListenPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
                IP: [
                    { required: true, message: '必填', trigger: 'blur' }
                ]
            }
        });

        const loadConfig = () => {
            getConfigure(plugin.config).then((res) => {
                state.configInfo = res;
                state.form.IP = res.IP;
                state.form.LanIPs = res.LanIPs.join('\n');
                state.form.ListenPort = res.ListenPort;
                state.form.BufferSize = res.BufferSize;
                state.form.ConnectEnable = res.ConnectEnable;
                state.form.BroadcastBind = res.BroadcastBind;
                state.form.BroadcastEnable = res.BroadcastEnable;
                state.form.BroadcastList = res.BroadcastList.join('\n');
            });
        }

        onMounted(() => {
            loadConfig();
        });
        const formDom = ref(null);
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    state.configInfo.IP = state.form.IP;
                    state.configInfo.LanIPs = state.form.LanIPs.splitStr();
                    state.configInfo.ListenPort = +state.form.ListenPort;
                    state.configInfo.BufferSize = +state.form.BufferSize;
                    state.configInfo.ConnectEnable = state.form.ConnectEnable;
                    state.configInfo.BroadcastBind = state.form.BroadcastBind;
                    state.configInfo.BroadcastEnable = state.form.BroadcastEnable;
                    state.configInfo.BroadcastList = state.form.BroadcastList.splitStr();
                    saveConfigure(plugin.config, JSON.stringify(state.configInfo)).then(resolve).catch(reject);
                });
            });
        }

        return {
            shareData, targets, state, formDom, submit
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-row {
    width: 100%;
}

.el-form-item:last-child {
    margin-bottom: 0;
}

@media screen and (max-width: 768px) {
    .el-col {
        margin-top: 0.6rem;
    }
}
</style>