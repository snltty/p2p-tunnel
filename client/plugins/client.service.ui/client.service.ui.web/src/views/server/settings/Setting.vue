<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="state.form" :rules="state.rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="连接限流" prop="ConnectLimit">
                                <el-tooltip class="box-item" effect="dark" content="连接限流,一分钟内，同ip可连接数，0不限制，需要重启" placement="top-start">
                                    <el-input size="default" v-model="state.form.ConnectLimit" placeholder="连接限流"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="bufsize" prop="TcpBufferSize">
                                <el-select size="default" v-model="state.form.TcpBufferSize" placeholder="选择合适的buff">
                                    <el-option v-for="(item,index) in shareData.bufferSizes" :key="index" :label="item" :value="index"></el-option>
                                </el-select>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="掉线超时" prop="TimeoutDelay">
                                <el-tooltip class="box-item" effect="dark" content="超时强制下线时间 ms" placement="top-start">
                                    <el-input size="default" v-model="state.form.TimeoutDelay" placeholder="掉线超时"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="登入超时" prop="RegisterTimeout">
                                <el-tooltip class="box-item" effect="dark" content="登入超时，tcp连接后，多久没登入，就断开" placement="top-start">
                                    <el-input size="default" v-model="state.form.RegisterTimeout" placeholder="登入超时"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="允许登入" prop="RegisterEnable">
                                <el-tooltip class="box-item" effect="dark" content="不允许登入则所有账号均不可登入" placement="top-start">
                                    <el-checkbox size="default" v-model="state.form.RegisterEnable">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="允许中继" prop="RelayEnable">
                                <el-tooltip class="box-item" effect="dark" content="允许所有账号使用中继" placement="top-start">
                                    <el-checkbox size="default" v-model="state.form.RelayEnable">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="加密秘钥" prop="EncodePassword">
                                <el-tooltip class="box-item" effect="dark" content="如果服务器填写了秘钥，客户端选择加密时，必须填写相同秘钥" placement="top-start">
                                    <el-input size="default" v-model="state.form.EncodePassword" placeholder="加密秘钥"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="管理分组" prop="AdminGroup">
                                <el-tooltip class="box-item" effect="dark" content="此分组下登录即为超级管理员" placement="top-start">
                                    <el-input size="default" type="password" show-password v-model="state.form.AdminGroup" placeholder="管理分组"></el-input>
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
import { ref, reactive } from '@vue/reactivity';
import { getConfigure, saveConfigure } from '../../../apis/configure'
import { shareData } from '../../../states/shareData'
import { onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    setup() {
        const formDom = ref(null);
        const state = reactive({
            form: {
                ConnectLimit: 0,
                TcpBufferSize: 0,
                TimeoutDelay: 0,
                RegisterTimeout: 0,
                RegisterEnable: true,
                RelayEnable: true,
                EncodePassword: '',
                AdminGroup: '',
            },
            rules: {
                ConnectLimit: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 0, max: 1000, message: '数字 0-1000', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
                TimeoutDelay: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1000, max: 3600000, message: '数字 1000-3600000', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
                RegisterTimeout: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1000, max: 3600000, message: '数字 1000-3600000', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });

        const loadConfig = () => {
            return new Promise((resolve, reject) => {
                getConfigure(plugin.config).then((json) => {
                    resolve(new Function(`return ${json}`)());
                }).catch(reject);
            });
        }
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    loadConfig().then((json) => {
                        json.ConnectLimit = +state.form.ConnectLimit;
                        json.TcpBufferSize = +state.form.TcpBufferSize;
                        json.TimeoutDelay = +state.form.TimeoutDelay;
                        json.RegisterTimeout = +state.form.RegisterTimeout;
                        json.RegisterEnable = state.form.RegisterEnable;
                        json.RelayEnable = state.form.RelayEnable;
                        json.EncodePassword = state.form.EncodePassword;
                        json.AdminGroup = state.form.AdminGroup;

                        saveConfigure(plugin.config, JSON.stringify(json)).then(resolve).catch(reject);
                    }).catch(reject);
                });
            })
        }

        onMounted(() => {
            loadConfig().then((json) => {
                if (json) {
                    state.form.ConnectLimit = json.ConnectLimit;
                    state.form.TcpBufferSize = json.TcpBufferSize;
                    state.form.TimeoutDelay = json.TimeoutDelay;
                    state.form.RegisterTimeout = json.RegisterTimeout;
                    state.form.RegisterEnable = json.RegisterEnable;
                    state.form.RelayEnable = json.RelayEnable;
                    state.form.EncodePassword = json.EncodePassword;
                    state.form.AdminGroup = json.AdminGroup;
                }
            });
        });

        return {
            shareData, state, formDom, submit
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