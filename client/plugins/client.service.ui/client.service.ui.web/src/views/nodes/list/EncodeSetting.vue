<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="P2P加密" prop="ClientEncode">
                                <el-tooltip class="box-item" effect="dark" content="客户端之间通信使用加密" placement="top-start">
                                    <el-checkbox v-model="model.ClientEncode">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="密钥" prop="ClientEncodePassword">
                                <el-tooltip class="box-item" effect="dark" content="加密密钥32位，为空则每次加密随机密钥，如果填写，则各客户端都填写" placement="top-start">
                                    <el-input size="default" type="password" show-password maxlength="32" show-word-limit v-model="model.ClientEncodePassword"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="注册加密" prop="ServerEncode">
                                <el-tooltip class="box-item" effect="dark" content="客户端与服务端之间通信使用加密" placement="top-start">
                                    <el-checkbox v-model="model.ServerEncode">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="密钥" prop="ServerEncodePassword">
                                <el-tooltip class="box-item" effect="dark" content="加密密钥 32位，为空则每次加密随机密钥，使用p2p.snltty.com服务器则必须留空" placement="top-start">
                                    <el-input size="default" type="password" show-password maxlength="32" show-word-limit v-model="model.ServerEncodePassword"></el-input>
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
import { getRegisterInfo, updateConfig } from '../../../apis/register'
import { onMounted } from '@vue/runtime-core';
export default {
    setup() {
        const formDom = ref(null);
        const state = reactive({
            model: {
                ClientEncode: false,
                ClientEncodePassword: "",
                ServerEncode: false,
                ServerEncodePassword: "",
            },
            rules: {
            }
        });

        const loadConfig = () => {
            getRegisterInfo().then((json) => {
                state.model.ClientEncode = json.ClientConfig.Encode;
                state.model.ClientEncodePassword = json.ClientConfig.EncodePassword;

                state.model.ServerEncode = json.ServerConfig.Encode;
                state.model.ServerEncodePassword = json.ServerConfig.EncodePassword;
            }).catch((msg) => {
            });
        }
        const getJson = () => {
            return new Promise((resolve, reject) => {
                getRegisterInfo().then((json) => {
                    json.ClientConfig.Encode = state.model.ClientEncode;
                    json.ClientConfig.EncodePassword = state.model.ClientEncodePassword;
                    json.ServerConfig.Encode = state.model.ServerEncode;
                    json.ServerConfig.EncodePassword = state.model.ServerEncodePassword;
                    resolve(json);
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
                    getJson().then((json) => {
                        updateConfig(json).then(resolve).catch(reject);
                    }).catch(reject);
                });
            });
        }

        onMounted(() => {
            loadConfig();
        });

        return {
            ...toRefs(state), formDom, submit
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