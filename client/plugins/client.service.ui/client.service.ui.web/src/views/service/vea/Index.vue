<!--
 * @Author: snltty
 * @Date: 2022-05-14 19:17:29
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-29 15:42:35
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
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="本机IP" prop="IP">
                                        <el-tooltip class="box-item" effect="dark" content="当前客户端的虚拟网卡ip，各个客户端之间设置不一样的ip，相同网段即可" placement="top-start">
                                            <el-input v-model="state.form.IP"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="局域网段" prop="LanIPs">
                                        <el-tooltip class="box-item" effect="dark" content="当前客户端的局域网段，各个客户端之间设置不一样的网段即可，192.168.x.0酱紫，为空不启用，多个网段用英文逗号间隔" placement="top-start">
                                            <el-input v-model="state.form.LanIPs"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label-width="0">
                        <div class="w-100 t-c">
                            <el-button type="primary" :loading="state.loading" @click="handleSubmit" class="m-r-1">确 定</el-button>
                            <ConfigureModal className="VeaClientConfigure">
                                <el-button>客户端配置</el-button>
                            </ConfigureModal>
                        </div>
                    </el-form-item>
                </el-form>
            </div>
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
import { computed, reactive, ref } from '@vue/reactivity'
import { getConfig, setConfig, getList, reset, update } from '../../../apis/vea'
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
                LanIPs: '',
                SocksPort: 5415,
                BufferSize: 8 * 1024,
                ConnectEnable: false
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
                c.veaIp = JSON.parse(JSON.stringify(state.veaClients[c.Id] || { IP: '', LanIPs: [] }));
                c.veaIp.LanIPs = c.veaIp.LanIPs.filter(c => c.length > 0);
            });
            return clientsState.clients;
        });

        const loadConfig = () => {
            getConfig().then((res) => {
                state.form.Enable = res.Enable;
                state.form.ProxyAll = res.ProxyAll;
                state.form.TargetName = res.TargetName;
                state.form.IP = res.IP;
                state.form.LanIPs = res.LanIPs.join(',');
                state.form.SocksPort = res.SocksPort;
                state.form.BufferSize = res.BufferSize;
                state.form.ConnectEnable = res.ConnectEnable;
            });
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

        onMounted(() => {
            handleUpdate();
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
                json.BufferSize = Number(json.BufferSize);
                json.LanIPs = json.LanIPs.split(',').filter(c => c.length > 0);
                setConfig(json).then((res) => {
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
        const handleResetVea = (row) => {
            state.loading = true;
            reset({ id: row.Id }).then((res) => {
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

        return {
            targets, shareData, registerState, state, showClients, formDom, handleSubmit, handleResetVea, handleUpdate
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