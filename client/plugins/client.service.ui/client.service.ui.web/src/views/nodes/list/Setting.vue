<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="节点名称" prop="Name">
                                <el-input size="default" v-model="model.Name" maxlength="32" show-word-limit placeholder="设置你的注册名称"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="所在分组" prop="GroupId">
                                <el-tooltip class="box-item" effect="dark" content="设置你的分组编号，两个客户端之间分组编号一致时相互可见" placement="top-start">
                                    <el-select size="default" v-model="model.GroupId" @change="handleGroupIdChange" allow-create clearable filterable default-first-option placeholder="选择或输入分组编号">
                                        <el-option v-for="(item,index) in model.GroupIds" :key="index" :label="item" :value="item">
                                            <div class="flex">
                                                <span>{{item}}</span>
                                                <span class="flex-1"></span>
                                                <span style="padding:1px 0 0 1rem" @click.stop="handleRemoveGroupId(index)">
                                                    <el-icon>
                                                        <CircleClose />
                                                    </el-icon>
                                                </span>
                                            </div>
                                        </el-option>
                                    </el-select>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <template v-for="(item1,index) in components" :key="index">
                    <component :is="item1" :args="model.Args" :ref="`setting_item_${index}`"></component>
                </template>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="掉线超时" prop="TimeoutDelay">
                                <el-tooltip class="box-item" effect="dark" content="多久时间无法连通则掉线ms,使用5的倍数" placement="top-start">
                                    <el-input size="default" v-model="model.TimeoutDelay" placeholder="掉线超时"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="udp限速" prop="UdpUploadSpeedLimit">
                                <el-tooltip class="box-item" effect="dark" content="udp发送速度限制（字节数,0不限制）" placement="top-start">
                                    <el-input size="default" v-model="model.UdpUploadSpeedLimit"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="自动登入" prop="AutoReg">
                                <el-checkbox size="default" v-model="model.AutoReg">开启</el-checkbox>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="bufsize" prop="TcpBufferSize">
                                <el-select size="default" v-model="model.TcpBufferSize" placeholder="选择合适的buff">
                                    <el-option v-for="(item,index) in shareData.bufferSizes" :key="index" :label="item" :value="index"></el-option>
                                </el-select>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="tcp打洞" prop="UseTcp">
                                <el-tooltip class="box-item" effect="dark" content="是否使用tcp打洞" placement="top-start">
                                    <el-checkbox v-model="model.UseTcp">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="udp打洞" prop="UseUdp">
                                <el-tooltip class="box-item" effect="dark" content="是否使用udp打洞" placement="top-start">
                                    <el-checkbox v-model="model.UseUdp">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="自动打洞" prop="UsePunchHole">
                                <el-tooltip class="box-item" effect="dark" content="发现新客户端后是否自动打洞" placement="top-start">
                                    <el-checkbox v-model="model.UsePunchHole">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="断线重连" prop="UseReConnect">
                                <el-tooltip class="box-item" effect="dark" content="客户端之间掉线后，是否尝试重新连接" placement="top-start">
                                    <el-checkbox v-model="model.UseReConnect">开启</el-checkbox>>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="自动中继" prop="AutoRelay">
                                <el-tooltip class="box-item" effect="dark" content="不开启自动打洞的话，是否自动中继" placement="top-start">
                                    <el-checkbox v-model="model.AutoRelay">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="中继节点" prop="UseRelay">
                                <el-tooltip class="box-item" effect="dark" content="是否允许本客户端作为中继节点" placement="top-start">
                                    <el-checkbox v-model="model.UseRelay">开启</el-checkbox>>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { ref, toRefs, reactive } from '@vue/reactivity';
import { getSignInInfo, updateConfig } from '../../../apis/signin'
import { injectServices, accessService } from '../../../states/services';
import { shareData } from '../../../states/shareData';
import { getCurrentInstance, onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: Object.assign(JSON.parse(JSON.stringify(plugin)), { text: '节点配置' }),
    setup() {

        const instance = getCurrentInstance();
        const servicesState = injectServices();
        const files = require.context('../../../', true, /Args\.vue/);
        const components = files.keys().map(c => files(c).default).filter(c => accessService(c.plugin.service, servicesState));

        const formDom = ref(null);
        const state = reactive({
            model: {
                Name: '',
                GroupId: '',
                Args: {},
                AutoReg: false,
                GroupIds: [],
                UsePunchHole: false,
                TimeoutDelay: 20000,
                UseUdp: false,
                UseTcp: false,
                UseRelay: true,
                AutoRelay: true,
                UseReConnect: false,
                UdpUploadSpeedLimit: 0,
                TcpBufferSize: 0,
            },
            rules: {
                Name: [{ required: true, message: '必填', trigger: 'blur' }],
                TimeoutDelay: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 2147483647, message: '数字 1-2147483647', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
                UdpUploadSpeedLimit: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 0, max: 2147483647, message: '数字 0-2147483647', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });

        const loadConfig = () => {
            getSignInInfo().then((json) => {
                state.model.TcpBufferSize = json.ClientConfig.TcpBufferSize;
                state.model.Name = json.ClientConfig.Name;
                state.model.Args = json.ClientConfig.Args;
                state.model.GroupId = json.ClientConfig.GroupId;
                state.model.GroupIds = json.ClientConfig.GroupIds;

                state.model.AutoReg = json.ClientConfig.AutoReg;
                state.model.UsePunchHole = json.ClientConfig.UsePunchHole;
                state.model.UseReConnect = json.ClientConfig.UseReConnect;

                state.model.TimeoutDelay = json.ClientConfig.TimeoutDelay;

                state.model.UseUdp = json.ClientConfig.UseUdp;
                state.model.UseTcp = json.ClientConfig.UseTcp;
                state.model.UseRelay = json.ClientConfig.UseRelay;
                state.model.AutoRelay = json.ClientConfig.AutoRelay;
                state.model.UdpUploadSpeedLimit = json.ClientConfig.UdpUploadSpeedLimit;
            }).catch((msg) => {
            });
        }

        const getJson = () => {
            return new Promise((resolve, reject) => {
                getSignInInfo().then((json) => {
                    json.ClientConfig.TcpBufferSize = +state.model.TcpBufferSize;
                    json.ClientConfig.Name = state.model.Name;
                    json.ClientConfig.Args = getArgs();
                    json.ClientConfig.GroupId = state.model.GroupId;
                    json.ClientConfig.GroupIds = state.model.GroupIds;
                    json.ClientConfig.AutoReg = state.model.AutoReg;
                    json.ClientConfig.UsePunchHole = state.model.UsePunchHole;
                    json.ClientConfig.TimeoutDelay = +state.model.TimeoutDelay;
                    json.ClientConfig.UseUdp = state.model.UseUdp;
                    json.ClientConfig.UseTcp = state.model.UseTcp;
                    json.ClientConfig.UseRelay = state.model.UseRelay;
                    json.ClientConfig.AutoRelay = state.model.AutoRelay;
                    json.ClientConfig.UseReConnect = state.model.UseReConnect;
                    json.ClientConfig.UdpUploadSpeedLimit = +state.model.UdpUploadSpeedLimit;
                    resolve(json);
                }).catch(reject);
            })
        }
        const getArgs = () => {
            const refs = instance.refs;
            const args = {};
            for (let j in refs) {
                if (j.indexOf('setting_item') == 0) {
                    Object.assign(args, refs[j][0].submit());
                }
            }
            return args;
        }
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    getJson().then((json) => {
                        updateConfig(json).then(resolve).catch(reject);
                    }).catch(reject);
                });
            })
        }

        const handleGroupIdChange = async (val) => {
            val = val.replace(/^\s|\s$/g, '');
            if (state.model.GroupIds.indexOf(val) == -1 && val) {
                state.model.GroupIds.push(val);
            }
            getJson().then((json) => {
                updateConfig(json);
            });
        }
        const handleRemoveGroupId = (index) => {
            state.model.GroupIds.splice(index, 1);
            getJson().then((json) => {
                updateConfig(json);
            });
        }

        onMounted(() => {
            loadConfig();
        });

        return {
            shareData, components, ...toRefs(state), formDom, submit, handleGroupIdChange, handleRemoveGroupId
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