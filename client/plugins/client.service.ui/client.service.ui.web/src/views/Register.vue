<!--
 * @Author: snltty
 * @Date: 2021-08-19 22:30:19
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-30 20:53:46
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\Register.vue
-->
<template>
    <div class="register-form">
        <h3 class="title t-c">将本客户端注册到服务器</h3>
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="名称" prop="ClientName">
                                <el-input v-model="model.ClientName" maxlength="32" show-word-limit placeholder="设置你的注册名称"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="分组" prop="GroupId">
                                <el-tooltip class="box-item" effect="dark" content="设置你的分组编号，两个客户端之间分组编号一致时相互可见" placement="top-start">
                                    <el-input v-model="model.GroupId" maxlength="32" show-word-limit placeholder="设置你的分组编号"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                            <el-form-item label="注册地址" prop="ServerIp">
                                <el-input v-model="model.ServerIp" placeholder="域名或IP地址"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                            <el-form-item label="udp端口" prop="ServerUdpPort">
                                <el-input v-model="model.ServerUdpPort"></el-input>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                            <el-form-item label="tcp端口" prop="ServerTcpPort">
                                <el-input v-model="model.ServerTcpPort"></el-input>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <!-- <el-form-item label="" label-width="0" class="w-100"> -->
                <el-collapse>
                    <el-collapse-item title="可选和其它" name="1">
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="本地udp" prop="UdpPort">
                                        <el-input readonly v-model="registerState.LocalInfo.UdpPort"></el-input>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="本地tcp" prop="TcpPort">
                                        <el-input readonly v-model="registerState.LocalInfo.TcpPort"></el-input>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="本地mac" prop="Mac">
                                        <el-input readonly v-model="registerState.LocalInfo.Mac"></el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="外网udp" prop="UdpPort">
                                        <el-input readonly v-model="registerState.RemoteInfo.UdpPort"></el-input>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="外网tcp" prop="TcpPort">
                                        <el-input readonly v-model="registerState.RemoteInfo.TcpPort"></el-input>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8">
                                    <el-form-item label="外网IP" prop="Ip">
                                        <el-input readonly v-model="registerState.RemoteInfo.Ip"></el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="自动注册" prop="AutoReg">
                                        <el-checkbox v-model="model.AutoReg">自动注册</el-checkbox>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="次数" prop="AutoRegTimes" label-width="40">
                                        <el-tooltip class="box-item" effect="dark" content="如果自动注册失败，将要重试几次" placement="top-start">
                                            <el-input v-model="model.AutoRegTimes"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="间隔" prop="AutoRegInterval" label-width="40">
                                        <el-tooltip class="box-item" effect="dark" content="间隔多久重试一次(ms)" placement="top-start">
                                            <el-input v-model="model.AutoRegInterval"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="延迟" prop="AutoRegDelay" label-width="40">
                                        <el-tooltip class="box-item" effect="dark" content="断线后多久重试" placement="top-start">
                                            <el-input v-model="model.AutoRegDelay"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="6" :sm="3" :md="3" :lg="3" :xl="3">
                                    <el-form-item label="p2p加密" prop="ClientEncode" label-width="60">
                                        <el-tooltip class="box-item" effect="dark" content="客户端之间通信使用加密" placement="top-start">
                                            <el-switch v-model="model.ClientEncode" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="18" :sm="9" :md="9" :lg="9" :xl="9">
                                    <el-form-item label="密钥" prop="ClientEncodePassword">
                                        <el-tooltip class="box-item" effect="dark" content="加密密钥32位，为空则每次加密随机密钥，如果填写，则各客户端都填写" placement="top-start">
                                            <el-input v-model="model.ClientEncodePassword"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="6" :sm="3" :md="3" :lg="3" :xl="3">
                                    <el-form-item label="注册加密" prop="ServerEncode" label-width="60">
                                        <el-tooltip class="box-item" effect="dark" content="客户端与服务端之间通信使用加密" placement="top-start">
                                            <el-switch v-model="model.ServerEncode" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="18" :sm="9" :md="9" :lg="9" :xl="9">
                                    <el-form-item label="密钥" prop="ServerEncodePassword">
                                        <el-tooltip class="box-item" effect="dark" content="加密密钥 32位，为空则每次加密随机密钥，使用p2p.snltty.com服务器则必须留空" placement="top-start">
                                            <el-input v-model="model.ServerEncodePassword"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                        <el-form-item label="" label-width="0">
                            <el-row>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="使用udp" prop="UseUdp" label-width="60">
                                        <el-tooltip class="box-item" effect="dark" content="是否使用udp" placement="top-start">
                                            <el-switch v-model="model.UseUdp" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                    <el-form-item label="使用tcp" prop="UseTcp" label-width="60">
                                        <el-tooltip class="box-item" effect="dark" content="是否使用tcp" placement="top-start">
                                            <el-switch v-model="model.UseTcp" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </el-form-item>
                    </el-collapse-item>
                </el-collapse>
                <!-- </el-form-item> -->

                <el-form-item label="" label-width="80">
                    <div class="t-c">
                        <el-row>
                            <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                                <el-form-item label="UDP" prop="UdpConnected">
                                    <el-switch disabled v-model="registerState.LocalInfo.UdpConnected">UDP</el-switch>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                                <el-form-item label="TCP" prop="TcpConnected">
                                    <el-switch disabled v-model="registerState.LocalInfo.TcpConnected">TCP</el-switch>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                                <el-form-item label="自动打洞" prop="AutoPunchHole">
                                    <el-tooltip class="box-item" effect="dark" content="发现新客户端后是否自动打洞" placement="top-start">
                                        <el-switch v-model="model.AutoPunchHole">TCP</el-switch>
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </div>
                </el-form-item>
                <el-form-item label="" label-width="0" class="t-c">
                    <div class="t-c w-100">
                        <el-button type="primary" size="large" :loading="registerState.LocalInfo.IsConnecting" @click="handleSubmit">注册</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { ref, toRefs, reactive } from '@vue/reactivity';
import { injectRegister } from '../states/register'
import { sendRegisterMsg, getRegisterInfo, updateConfig } from '../apis/register'

import { ElMessage } from 'element-plus'
import { watch } from '@vue/runtime-core';
export default {
    setup () {
        const formDom = ref(null);
        const registerState = injectRegister();
        const state = reactive({
            model: {
                ClientName: '',
                ServerIp: '',
                ServerUdpPort: 0,
                ServerTcpPort: 0,
                AutoReg: false,
                AutoRegTimes: 10,
                AutoRegInterval: 5000,
                AutoRegDelay: 5000,
                UseMac: false,
                GroupId: '',
                ClientEncode: false,
                ClientEncodePassword: "",
                ServerEncode: false,
                ServerEncodePassword: "",
                AutoPunchHole: false,
                UseUdp: false,
                UseTcp: false,
            },
            rules: {
                ClientName: [{ required: true, message: '必填', trigger: 'blur' }],
                ServerIp: [{ required: true, message: '必填', trigger: 'blur' }],
                AutoRegTimes: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 2147483647, message: '数字 1-2147483647', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                AutoRegInterval: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 2147483647, message: '数字 1-2147483647', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                AutoRegDelay: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 2147483647, message: '数字 1-2147483647', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                ServerUdpPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                ServerTcpPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });

        //获取一下可修改的数据
        getRegisterInfo().then((json) => {
            state.model.ClientName = registerState.ClientConfig.Name = json.ClientConfig.Name;
            state.model.GroupId = registerState.ClientConfig.GroupId = json.ClientConfig.GroupId;
            state.model.AutoReg = registerState.ClientConfig.AutoReg = json.ClientConfig.AutoReg;
            state.model.AutoRegTimes = registerState.ClientConfig.AutoRegTimes = json.ClientConfig.AutoRegTimes;
            state.model.AutoRegInterval = registerState.ClientConfig.AutoRegInterval = json.ClientConfig.AutoRegInterval;
            state.model.AutoRegDelay = registerState.ClientConfig.AutoRegDelay = json.ClientConfig.AutoRegDelay;
            state.model.UseMac = registerState.ClientConfig.UseMac = json.ClientConfig.UseMac;
            state.model.ClientEncode = registerState.ClientConfig.Encode = json.ClientConfig.Encode;
            state.model.ClientEncodePassword = registerState.ClientConfig.ClientEncodePassword = json.ClientConfig.EncodePassword;
            state.model.AutoPunchHole = registerState.ClientConfig.AutoPunchHole = json.ClientConfig.AutoPunchHole;
            state.model.UseUdp = registerState.ClientConfig.UseUdp = json.ClientConfig.UseUdp;
            state.model.UseTcp = registerState.ClientConfig.UseTcp = json.ClientConfig.UseTcp;

            state.model.ServerIp = registerState.ServerConfig.Ip = json.ServerConfig.Ip;
            state.model.ServerUdpPort = registerState.ServerConfig.UdpPort = json.ServerConfig.UdpPort;
            state.model.ServerTcpPort = registerState.ServerConfig.TcpPort = json.ServerConfig.TcpPort;
            state.model.ServerEncode = registerState.ServerConfig.Encode = json.ServerConfig.Encode;
            state.model.ServerEncodePassword = registerState.ServerConfig.ServerEncodePassword = json.ServerConfig.EncodePassword;
        }).catch((msg) => {
            // ElMessage.error(msg);
        });
        watch(() => registerState.ClientConfig.GroupId, () => {
            state.model.GroupId = registerState.ClientConfig.GroupId;
        });

        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                let data = {
                    ClientConfig: {
                        Name: state.model.ClientName,
                        GroupId: state.model.GroupId,
                        AutoReg: state.model.AutoReg,
                        AutoRegTimes: +state.model.AutoRegTimes,
                        AutoRegInterval: +state.model.AutoRegInterval,
                        AutoRegDelay: +state.model.AutoRegDelay,
                        UseMac: state.model.UseMac,
                        Encode: state.model.ClientEncode,
                        EncodePassword: state.model.ClientEncodePassword,
                        AutoPunchHole: state.model.AutoPunchHole,
                        UseUdp: state.model.UseUdp,
                        UseTcp: state.model.UseTcp,
                    },
                    ServerConfig: {
                        Ip: state.model.ServerIp,
                        UdpPort: +state.model.ServerUdpPort,
                        TcpPort: +state.model.ServerTcpPort,
                        Encode: state.model.ServerEncode,
                        EncodePassword: state.model.ServerEncodePassword
                    }
                };
                registerState.LocalInfo.IsConnecting = true;
                updateConfig(data).then(() => {
                    sendRegisterMsg().then((res) => {
                    }).catch((msg) => {
                        ElMessage.error(msg);
                    });
                }).catch((msg) => {
                    ElMessage.error(msg);
                })
            });
        }

        return {
            ...toRefs(state), registerState, formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-row
    width: 100%;

.register-form
    padding: 2rem;

    .inner
        border: 1px solid #eee;
        padding: 2rem;
        border-radius: 4px;

@media screen and (max-width: 768px)
    .el-col
        margin-top: 0.6rem;
</style>