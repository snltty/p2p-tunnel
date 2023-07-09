<template>
    <el-dialog append-to-body title="添加长连接(tcp,udp)" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="350px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="100px">
            <el-form-item label="服务器端口" prop="ServerPort">
                <el-input v-model="state.form.ServerPort"></el-input>
            </el-form-item>
            <el-form-item label="本机服务" prop="LocalIp">
                <el-row>
                    <el-col :xs="16" :sm="16" :md="16" :lg="16" :xl="16">
                        <el-form-item label="" prop="LocalIp">
                            <el-input v-model="state.form.LocalIp"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :xs="8" :sm="8" :md="8" :lg="8" :xl="8">
                        <el-form-item label="" prop="LocalPort">
                            <el-tooltip class="box-item" effect="dark" content="端口" placement="top-start">
                                <el-input v-model="state.form.LocalPort"></el-input>
                            </el-tooltip>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="简单说明" prop="Desc">
                <el-input v-model="state.form.Desc"></el-input>
            </el-form-item>
        </el-form>
        <div class="remark t-c" v-html="remark"></div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" :loading="state.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity';
import { inject, watch } from '@vue/runtime-core';
import { AddServerForward } from '../../../../../apis/forward-server'
import { injectShareData } from '../../../../../states/shareData'
import { injectSignIn } from '../../../../../states/signin'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {

        const signinState = injectSignIn();
        const addListenData = inject('add-listen-data');
        const shareData = injectShareData();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            form: {
                AliveType: addListenData.value.AliveType,
                ServerPort: 0,
                Domain: signinState.ServerConfig.Ip,
                Desc: '',
                LocalIp: '127.0.0.1',
                LocalPort: 80
            },
            rules: {
                Domain: [{ required: true, message: '必填', trigger: 'blur' }],
                LocalIp: [{ required: true, message: '必填', trigger: 'blur' }],
                ServerPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
                LocalPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const remark = computed(() => {
            return [
                `服务器(${signinState.ServerConfig.Ip}:${state.form.ServerPort})`,
                ` -> `,
                `本机(${state.form.LocalIp}:${state.form.LocalPort})`
            ].join('');
        });

        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;

                let json = JSON.parse(JSON.stringify(state.form));
                json.ServerPort = Number(json.ServerPort);
                json.AliveType = Number(json.AliveType);
                json.LocalPort = Number(json.LocalPort);
                AddServerForward(json).then((res) => {
                    state.loading = false;
                    if (res) {
                        state.show = false;
                        emit('success');
                    }
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }
        const handleCancel = () => {
            state.show = false;
        }

        return {
            shareData, state, formDom, remark, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped>
.remark {
    margin-top: 1rem;
}
</style>