<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="state.form" :rules="state.rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="开启" prop="ConnectEnable">
                                <el-tooltip class="box-item" effect="dark" content="允许所有账号使用端口转发穿透，包括匿名" placement="top-start">
                                    <el-checkbox size="default" v-model="state.form.ConnectEnable">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="bufsize" prop="BufferSize">
                                <el-select size="default" v-model="state.form.BufferSize" placeholder="选择合适的buff">
                                    <el-option v-for="(item,index) in shareData.bufferSizes" :key="index" :label="item" :value="index"></el-option>
                                </el-select>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
                            <el-form-item label="短链接端口" prop="WebListens">
                                <el-tooltip class="box-item" effect="dark" content="短链接端口列表，多个英文逗号间隔" placement="top-start">
                                    <el-input size="default" v-model="state.form.WebListens" placeholder="短链接端口"></el-input>
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
                                    <el-input size="default" v-model="state.form.min" placeholder="长连接端口开始"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="端口结束" prop="max">
                                <el-tooltip class="box-item" effect="dark" content="长连接端口结束" placement="top-start">
                                    <el-input size="default" v-model="state.form.max" placeholder="长连接端口结束"></el-input>
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
                ConnectEnable: true,
                WebListens: "",
                BufferSize: 3,
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
                        json.ConnectEnable = state.form.ConnectEnable;
                        json.BufferSize = +state.form.BufferSize;
                        json.WebListens = state.form.WebListens.split(',').filter(c => c.length > 0).map(c => +c);
                        json.TunnelListenRange.Min = +state.form.min;
                        json.TunnelListenRange.Max = +state.form.max;
                        saveConfigure(plugin.config, JSON.stringify(json)).then(resolve).catch(reject);
                    }).catch(reject);
                });
            })
        }

        onMounted(() => {
            loadConfig().then((json) => {
                state.form.ConnectEnable = json.ConnectEnable;
                state.form.BufferSize = json.BufferSize;
                state.form.WebListens = json.WebListens.join(',');
                state.form.min = json.TunnelListenRange.Min;
                state.form.max = json.TunnelListenRange.Max;
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