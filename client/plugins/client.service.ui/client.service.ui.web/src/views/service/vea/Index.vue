<!--
 * @Author: snltty
 * @Date: 2022-05-14 19:17:29
 * @LastEditors: snltty
 * @LastEditTime: 2022-09-04 13:04:50
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\vea\Index.vue
-->
<template>
    <div class="socks5-wrap">
        <div class="inner">
            <h3 class="title t-c">{{$route.meta.name}}</h3>
            <el-alert class="alert" type="warning" show-icon :closable="false" title="虚拟网卡组网，可将在线客户端组合成一个网络，然后通过客户端ip直接访问，暂时仅windows" description="需要管理员运行，否则无法添加虚拟网卡" />
            <div class="form">
                <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="代理端口" prop="SocksPort">
                                        <el-tooltip class="box-item" effect="dark" content="代理端口，无所谓，填写一个未被占用的端口即可" placement="top-start">
                                            <el-input v-model="state.form.SocksPort"></el-input>
                                        </el-tooltip>

                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="buffersize" prop="BufferSize">
                                        <el-input v-model="state.form.BufferSize"></el-input>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="通信通道" prop="TunnelType">
                                        <el-select v-model="state.form.TunnelType" placeholder="选择类型">
                                            <el-option v-for="(item,index) in shareData.tunnelTypes" :key="index" :label="item" :value="index">
                                            </el-option>
                                        </el-select>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="目标端" prop="TargetName">
                                        <el-tooltip class="box-item" effect="dark" content="当遇到不存在的ip时，目标端应该选择谁，为某个客户端" placement="top-start">
                                            <el-select v-model="state.form.TargetName" placeholder="选择目标">
                                                <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
                                                </el-option>
                                            </el-select>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label-width="0" prop="Enable">
                                        <el-tooltip class="box-item" effect="dark" content="不开启，则只修改配置信息，不安装虚拟网卡" placement="top-start">
                                            <el-checkbox v-model="state.form.Enable" label="开启网卡" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label-width="0" prop="ProxyAll">
                                        <el-tooltip class="box-item" effect="dark" content="是否由虚拟网卡代理所有，暂不可用" placement="top-start">
                                            <el-checkbox disabled v-model="state.form.ProxyAll" label="代理所有" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label-width="0" prop="ConnectEnable">
                                        <el-tooltip class="box-item" effect="dark" content="作为被访问端时，是否允许访问" placement="top-start">
                                            <el-checkbox v-model="state.form.ConnectEnable" label="允许访问" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label-width="0" prop="LanConnectEnable">
                                        <el-tooltip class="box-item" effect="dark" content="作为被访问端时，是否允许访问本地地址，虚拟IP为本地地址" placement="top-start">
                                            <el-checkbox v-model="state.form.LanConnectEnable" label="允许访问本地" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label="本机IP" prop="IP">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-tooltip class="box-item" effect="dark" content="当前客户端的虚拟网卡ip，各个客户端之间设置不一样的ip，相同网段即可" placement="top-start">
                                        <el-input :readonly="registerState.LocalInfo.connected" v-model="state.form.IP"></el-input>
                                    </el-tooltip>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label-width="0">
                        <div class="w-100 t-c">
                            <el-button type="primary" :loading="state.loading" @click="handleSubmit" class="m-r-1">确 定</el-button>
                            <ConfigureModal className="VeaClientConfigure">
                                <el-button>配置插件</el-button>
                            </ConfigureModal>
                        </div>
                    </el-form-item>
                </el-form>
            </div>
        </div>
        <div class="inner">
            <h3 class="title t-c">组网列表</h3>
            <div>
                <el-table border :data="showClients" style="width: 100%">
                    <el-table-column prop="Name" label="客户端">
                        <template #default="scope">
                            <strong v-if="scope.row.online" style="color:green">{{scope.row.Name}}</strong>
                            <span v-else>{{scope.row.Name}}</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="veaIp" label="虚拟ip" />
                </el-table>
            </div>
        </div>
    </div>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity'
import { getConfig, setConfig, getUpdate } from '../../../apis/vea'
import { onMounted, onUnmounted } from '@vue/runtime-core'
import { ElMessage } from 'element-plus'
import { injectClients } from '../../../states/clients'
import { injectShareData } from '../../../states/shareData'
import { injectRegister } from '../../../states/register'
import { websocketState } from '../../../apis/request'
import ConfigureModal from '../configure/ConfigureModal.vue'
export default {
    components: { ConfigureModal },
    setup () {

        const clientsState = injectClients();
        const registerState = injectRegister();
        const shareData = injectShareData();
        const targets = computed(() => {
            return clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            });
        });
        const state = reactive({
            loading: false,
            form: {
                Enable: false,
                ProxyAll: false,
                TargetName: '',
                IP: '',
                TunnelType: '8',
                SocksPort: 5415,
                BufferSize: 8 * 1024,
                ConnectEnable: false,
                LanConnectEnable: false,
            },
            rules: {
                BufferSize: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1024, max: 65536, message: '数字 1k-64k', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                SocksPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                IP: [
                    { required: true, message: '必填', trigger: 'blur' }
                ]
            },
            veaClients: {}
        });
        const formDom = ref(null);
        const showClients = computed(() => {
            clientsState.clients.forEach(c => {
                c.veaIp = state.veaClients[c.Id] || '';
            });
            return clientsState.clients;
        });

        const loadConfig = () => {
            getConfig().then((res) => {
                state.form.Enable = res.Enable;
                state.form.ProxyAll = res.ProxyAll;
                state.form.TargetName = res.TargetName;
                state.form.IP = res.IP;
                state.form.TunnelType = res.TunnelType.toString();
                state.form.SocksPort = res.SocksPort;
                state.form.BufferSize = res.BufferSize;
                state.form.ConnectEnable = res.ConnectEnable;
                state.form.LanConnectEnable = res.LanConnectEnable;
            });
        }

        let timer = 0;
        const loadVeaClients = () => {
            if (websocketState.connected) {
                getUpdate().then((res) => {
                    state.veaClients = res;
                    timer = setTimeout(loadVeaClients, 1000);
                });
            } else {
                state.veaClients = {};
                timer = setTimeout(loadVeaClients, 1000);
            }
        }

        onMounted(() => {
            loadConfig();
            loadVeaClients();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        });

        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;

                const json = JSON.parse(JSON.stringify(state.form));
                json.SocksPort = Number(json.SocksPort);
                json.TunnelType = Number(json.TunnelType);
                json.BufferSize = Number(json.BufferSize);
                setConfig(json).then(() => {
                    state.loading = false;
                    if (json.IsPac) {
                        savePac();
                    }
                    ElMessage.success('操作成功！');
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }

        return {
            targets, shareData, registerState, state, showClients, formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped>
.socks5-wrap
    padding: 2rem;

.inner
    border: 1px solid #ddd;
    padding: 1rem;
    border-radius: 0.4rem;
    margin-bottom: 1rem;

.alert
    margin-bottom: 1rem;

@media screen and (max-width: 768px)
    .el-col
        margin-top: 0.6rem;
</style>