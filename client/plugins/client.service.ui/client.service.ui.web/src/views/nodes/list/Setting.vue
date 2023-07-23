<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="state.form" :rules="state.rules">

                <el-collapse v-model="activeNames">
                    <el-collapse-item title="主配置" name="1">
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="节点名称" prop="Name">
                                        <el-input size="default" v-model="state.form.Name" maxlength="32" show-word-limit placeholder="设置你的注册名称">
                                            <template #append>
                                                <el-tooltip class="box-item" effect="dark" content="设置你的注册名称" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                        </el-input>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="所在分组" prop="GroupId">
                                        <el-select size="default" v-model="state.form.GroupId" @change="handleGroupIdChange" allow-create clearable filterable default-first-option placeholder="选择或输入分组编号">
                                            <template #prefix>
                                                <el-tooltip class="box-item" effect="dark" content="设置你的分组编号，两个节点之间分组编号一致时相互可见" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                            <el-option v-for="(item,index) in state.form.GroupIds" :key="index" :label="item" :value="item">
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
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <template v-for="(item1,index) in components" :key="index">
                            <component :is="item1" :args="state.form.Args" :ref="`setting_item_${index}`"></component>
                        </template>
                    </el-collapse-item>
                    <el-collapse-item title="可选配置" name="2">
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="自动登入" prop="AutoReg">
                                        <el-checkbox size="default" v-model="state.form.AutoReg">开启
                                            <el-tooltip class="box-item" effect="dark" content="开启自动登入，节点启动后，立即连接服务端" placement="top">
                                                <el-icon>
                                                    <Warning />
                                                </el-icon>
                                            </el-tooltip></el-checkbox>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="自动打洞" prop="UsePunchHole">
                                        <el-checkbox v-model="state.form.UsePunchHole">开启
                                            <el-tooltip class="box-item" effect="dark" content="发现新节点后是否自动打洞" placement="top">
                                                <el-icon>
                                                    <Warning />
                                                </el-icon>
                                            </el-tooltip>
                                        </el-checkbox>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <high-config>
                            <el-form-item label="" label-width="0">
                                <el-row>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="tcp打洞" prop="UseTcp">
                                            <el-checkbox v-model="state.form.UseTcp">开启
                                                <el-tooltip class="box-item" effect="dark" content="是否使用tcp打洞" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </el-checkbox>
                                        </el-form-item>
                                    </el-col>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="udp打洞" prop="UseUdp">
                                            <el-checkbox v-model="state.form.UseUdp">开启
                                                <el-tooltip class="box-item" effect="dark" content="是否使用udp打洞" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </el-checkbox>
                                        </el-form-item>
                                    </el-col>
                                </el-row>
                            </el-form-item>
                        </high-config>
                        <high-config>
                            <el-form-item label="" label-width="0">
                                <el-row>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="自动中继" prop="AutoRelay">
                                            <el-checkbox v-model="state.form.AutoRelay">开启
                                                <el-tooltip class="box-item" effect="dark" content="不开启自动打洞的话，是否自动中继" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </el-checkbox>
                                        </el-form-item>
                                    </el-col>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="中继节点" prop="UseRelay">
                                            <el-checkbox v-model="state.form.UseRelay">开启
                                                <el-tooltip class="box-item" effect="dark" content="是否允许本节点作为中继节点" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </el-checkbox>
                                        </el-form-item>
                                    </el-col>
                                </el-row>
                            </el-form-item>
                        </high-config>
                        <high-config>
                            <el-form-item label="" label-width="0">
                                <el-row>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="bufsize" prop="TcpBufferSize">
                                            <el-select size="default" v-model="state.form.TcpBufferSize" placeholder="选择合适的buff">
                                                <el-option v-for="(item,index) in shareData.bufferSizes" :key="index" :label="item" :value="index"></el-option>
                                            </el-select>
                                        </el-form-item>
                                    </el-col>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="附加TTL" prop="TTL">
                                            <el-input size="default" v-model="state.form.TTL">
                                                <template #append>
                                                    <el-popover placement="top-start" title="TCP打洞可以调整TTL" :width="300" trigger="hover" content="TCP打洞，有一方将会以一个低TTL值向对方发起连接，达到在网关中留下对方信息，而不会被对方拒绝的效果，默认值1，表示你当前设备与外网的距离+1">
                                                        <template #reference>
                                                            <el-icon>
                                                                <Warning />
                                                            </el-icon>
                                                        </template>
                                                    </el-popover>
                                                </template>
                                            </el-input>
                                        </el-form-item>
                                    </el-col>
                                </el-row>
                            </el-form-item>
                        </high-config>
                        <high-config>
                            <el-form-item label="" label-width="0">
                                <el-row>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="掉线超时" prop="TimeoutDelay">
                                            <el-input size="default" v-model="state.form.TimeoutDelay" placeholder="掉线超时">
                                                <template #append>
                                                    <el-tooltip class="box-item" effect="dark" content="多久时间无法连通则掉线ms,使用5的倍数" placement="top">
                                                        <el-icon>
                                                            <Warning />
                                                        </el-icon>
                                                    </el-tooltip>
                                                </template>
                                            </el-input>
                                        </el-form-item>
                                    </el-col>
                                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                        <el-form-item label="udp限速" prop="UdpUploadSpeedLimit">
                                            <el-input size="default" v-model="state.form.UdpUploadSpeedLimit">
                                                <template #append>
                                                    <el-tooltip class="box-item" effect="dark" content="udp发送速度限制（字节数,0不限制）" placement="top">
                                                        <el-icon>
                                                            <Warning />
                                                        </el-icon>
                                                    </el-tooltip>
                                                </template>
                                            </el-input>
                                        </el-form-item>
                                    </el-col>
                                </el-row>
                            </el-form-item>
                        </high-config>
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="P2P加密" prop="ClientEncode">
                                        <el-checkbox v-model="state.form.ClientEncode">开启
                                            <el-tooltip class="box-item" effect="dark" content="节点之间通信使用加密" placement="top">
                                                <el-icon>
                                                    <Warning />
                                                </el-icon>
                                            </el-tooltip>
                                        </el-checkbox>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="密钥" prop="ClientEncodePassword">
                                        <el-input size="default" type="password" show-password maxlength="32" show-word-limit v-model="state.form.ClientEncodePassword" placeholder="加密密钥32位">
                                            <template #append>
                                                <el-tooltip class="box-item" effect="dark" content="加密密钥32位，为空则每次加密随机密钥，如果填写，则各节点都填写" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                        </el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="登入加密" prop="ServerEncode">
                                        <el-checkbox v-model="state.form.ServerEncode">开启
                                            <el-tooltip class="box-item" effect="dark" content="节点与服务端之间通信使用加密" placement="top">
                                                <el-icon>
                                                    <Warning />
                                                </el-icon>
                                            </el-tooltip>
                                        </el-checkbox>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="密钥" prop="ServerEncodePassword">
                                        <el-input size="default" type="password" show-password maxlength="32" show-word-limit v-model="state.form.ServerEncodePassword" placeholder="加密密钥32位">
                                            <template #append>
                                                <el-tooltip class="box-item" effect="dark" content="加密密钥32位，为空则每次加密随机密钥，使用默认服务器则必须留空" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                        </el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="UI密码" prop="UIPassword">
                                        <el-input size="default" type="password" show-password maxlength="32" show-word-limit v-model="state.form.UIPassword" placeholder="UI密码">
                                            <template #append>
                                                <el-tooltip class="box-item" effect="dark" content="不为空则需要输入此密码才能进行UI操作" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                        </el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                    </el-collapse-item>
                </el-collapse>
            </el-form>
        </div>
    </div>
</template>

<script>
import { ref, reactive } from '@vue/reactivity';
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

        const activeNames = ref(['1', '2']);

        const formDom = ref(null);
        const state = reactive({
            form: {
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
                TTL: 1,
                UdpUploadSpeedLimit: 0,
                TcpBufferSize: 0,
                HighConfig: false,
                UIPassword: '',

                ClientEncode: false,
                ClientEncodePassword: "",
                ServerEncode: false,
                ServerEncodePassword: "",
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
                ],
                TTL: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: -255, max: 255, message: '数字 -255-255', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });

        const loadConfig = () => {
            getSignInInfo().then((json) => {
                state.form.TcpBufferSize = json.ClientConfig.TcpBufferSize;
                state.form.Name = json.ClientConfig.Name;
                state.form.Args = json.ClientConfig.Args;
                state.form.GroupId = json.ClientConfig.GroupId;
                state.form.GroupIds = json.ClientConfig.GroupIds;

                state.form.AutoReg = json.ClientConfig.AutoReg;
                state.form.UsePunchHole = json.ClientConfig.UsePunchHole;
                state.form.TTL = json.ClientConfig.TTL;

                state.form.TimeoutDelay = json.ClientConfig.TimeoutDelay;

                state.form.UseUdp = json.ClientConfig.UseUdp;
                state.form.UseTcp = json.ClientConfig.UseTcp;
                state.form.UseRelay = json.ClientConfig.UseRelay;
                state.form.AutoRelay = json.ClientConfig.AutoRelay;
                state.form.UdpUploadSpeedLimit = json.ClientConfig.UdpUploadSpeedLimit;
                state.form.HighConfig = json.ClientConfig.HighConfig;
                state.form.UIPassword = json.ClientConfig.UIPassword;

                state.form.ClientEncode = json.ClientConfig.Encode;
                state.form.ClientEncodePassword = json.ClientConfig.EncodePassword;

                state.form.ServerEncode = json.ServerConfig.Encode;
                state.form.ServerEncodePassword = json.ServerConfig.EncodePassword;
            }).catch((msg) => {
            });
        }

        const getJson = () => {
            return new Promise((resolve, reject) => {
                getSignInInfo().then((json) => {
                    json.ClientConfig.TcpBufferSize = +state.form.TcpBufferSize;
                    json.ClientConfig.Name = state.form.Name;
                    json.ClientConfig.Args = getArgs();
                    json.ClientConfig.GroupId = state.form.GroupId;
                    json.ClientConfig.GroupIds = state.form.GroupIds;
                    json.ClientConfig.AutoReg = state.form.AutoReg;
                    json.ClientConfig.UsePunchHole = state.form.UsePunchHole;
                    json.ClientConfig.TimeoutDelay = +state.form.TimeoutDelay;
                    json.ClientConfig.UseUdp = state.form.UseUdp;
                    json.ClientConfig.UseTcp = state.form.UseTcp;
                    json.ClientConfig.UseRelay = state.form.UseRelay;
                    json.ClientConfig.AutoRelay = state.form.AutoRelay;
                    json.ClientConfig.TTL = +state.form.TTL;
                    json.ClientConfig.UdpUploadSpeedLimit = +state.form.UdpUploadSpeedLimit;
                    json.ClientConfig.HighConfig = state.form.HighConfig;
                    json.ClientConfig.UIPassword = state.form.UIPassword;


                    json.ClientConfig.Encode = state.form.ClientEncode;
                    json.ClientConfig.EncodePassword = state.form.ClientEncodePassword;
                    json.ServerConfig.Encode = state.form.ServerEncode;
                    json.ServerConfig.EncodePassword = state.form.ServerEncodePassword;
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
            if (state.form.GroupIds.indexOf(val) == -1 && val) {
                state.form.GroupIds.push(val);
            }
            getJson().then((json) => {
                updateConfig(json);
            });
        }
        const handleRemoveGroupId = (index) => {
            state.form.GroupIds.splice(index, 1);
            getJson().then((json) => {
                updateConfig(json);
            });
        }

        onMounted(() => {
            loadConfig();
        });

        return {
            shareData, activeNames, components, state, formDom, submit, handleGroupIdChange, handleRemoveGroupId
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