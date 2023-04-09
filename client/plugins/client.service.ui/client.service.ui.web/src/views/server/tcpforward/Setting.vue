<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="开启" prop="ConnectEnable">
                                <el-tooltip class="box-item" effect="dark" content="允许所有账号使用tcp穿透，包括匿名" placement="top-start">
                                    <el-checkbox size="default" v-model="model.ConnectEnable">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="短链接端口" prop="WebListens">
                                <el-tooltip class="box-item" effect="dark" content="短链接端口列表，多个英文逗号间隔" placement="top-start">
                                    <el-input size="default" v-model="model.WebListens" placeholder="短链接端口"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="端口开始" prop="min">
                                <el-tooltip class="box-item" effect="dark" content="长连接端口开始" placement="top-start">
                                    <el-input size="default" v-model="model.min" placeholder="长连接端口开始"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="端口结束" prop="max">
                                <el-tooltip class="box-item" effect="dark" content="长连接端口结束" placement="top-start">
                                    <el-input size="default" v-model="model.max" placeholder="长连接端口结束"></el-input>
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
import { getConfigure, saveConfigure } from '../../../apis/configure'
import { onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    setup() {
        const formDom = ref(null);
        const state = reactive({
            model: {
                ConnectEnable: true,
                WebListens: "",
                min: 1025,
                max: 65535
            },
            rules: {
                min: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1025, max: 65535, message: '数字 1025-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
                max: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1025, max: 65535, message: '数字 1025-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });

        const loadConfig = () => {
            return new Promise((resolve, reject) => {
                getConfigure('ServerTcpForwardConfigure').then((json) => {
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
                        json.ConnectEnable = state.model.ConnectEnable;
                        json.WebListens = state.model.WebListens.split(',').filter(c => c.length > 0).map(c => +c);
                        json.TunnelListenRange.Min = +state.model.min;
                        json.TunnelListenRange.Max = +state.model.max;
                        saveConfigure('ServerTcpForwardConfigure', JSON.stringify(json)).then(resolve).catch(reject);
                    }).catch(reject);
                });
            })
        }

        onMounted(() => {
            loadConfig().then((json) => {
                state.model.ConnectEnable = json.ConnectEnable;
                state.model.WebListens = json.WebListens.join(',');
                state.model.min = json.TunnelListenRange.Min;
                state.model.max = json.TunnelListenRange.Max;
            });
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