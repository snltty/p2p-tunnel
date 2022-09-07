<!--
 * @Author: snltty
 * @Date: 2022-03-24 15:15:31
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-18 16:54:53
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\server\tcpforward\AddForward.vue
-->
<template>
    <el-dialog title="添加短连接转发" destroy-on-close v-model="show" center :close-on-click-modal="false" width="350px">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="100px">
            <el-form-item label="服务器域名" prop="Domain">
                <el-input v-model="form.Domain"></el-input>
            </el-form-item>
            <el-form-item label="本机ip" prop="LocalIp">
                <el-input v-model="form.LocalIp"></el-input>
            </el-form-item>
            <el-form-item label="本机端口" prop="LocalPort">
                <el-input v-model="form.LocalPort"></el-input>
            </el-form-item>
            <el-form-item label="通信通道" prop="TunnelType">
                <el-select v-model="form.TunnelType" placeholder="选择通信通道">
                    <el-option v-for="(item,key) in shareData.tunnelTypes" :key="key" :label="item" :value="key">
                    </el-option>
                </el-select>
            </el-form-item>
            <el-form-item label="简单说明" prop="Desc">
                <el-input v-model="form.Desc"></el-input>
            </el-form-item>
        </el-form>
        <div class="remark t-c" v-html="remark"></div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, reactive, ref, toRefs } from '@vue/reactivity';
import { inject, watch } from '@vue/runtime-core';
import { AddServerForward } from '../../../apis/tcp-forward'
import { injectShareData } from '../../../states/shareData'
import { injectRegister } from '../../../states/register'
import { ElMessage } from 'element-plus/lib/components';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup (props, { emit }) {

        const registerState = injectRegister();
        const addForwardData = inject('add-forward-data');
        const shareData = injectShareData();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            form: {
                AliveType: addForwardData.value.AliveType,
                ServerPort: addForwardData.value.ServerPort,
                Domain: registerState.ServerConfig.Ip,
                Desc: '',
                LocalIp: '127.0.0.1',
                LocalPort: 80,
                TunnelType: '2',
            },
            rules: {
                Domain: [{ required: true, message: '必填', trigger: 'blur' }],
                LocalIp: [{ required: true, message: '必填', trigger: 'blur' }],
                LocalPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
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
                `服务器(${state.form.Domain}:${state.form.ServerPort})`,
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
                json.AliveType = Number(json.AliveType);
                json.LocalPort = Number(json.LocalPort);
                json.TunnelType = Number(json.TunnelType);
                AddServerForward(json).then((res) => {
                    state.loading = false;
                    if (res) {
                        ElMessage.error(res);
                    } else {
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
            shareData, ...toRefs(state), formDom, remark, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped>
.remark
    margin-top: 1rem;
</style>