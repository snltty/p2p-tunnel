<!--
 * @Author: snltty
 * @Date: 2022-03-24 15:15:31
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-06 14:06:55
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\udpforward\AddListen.vue
-->
<template>
    <el-dialog :title="form.ID > 0?'编辑监听':'新增监听'" destroy-on-close v-model="show" center :close-on-click-modal="false" width="500px">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="80px">
            <el-form-item label="监听端口" prop="Port">
                <el-input v-model="form.Port" :readonly="form.ID > 0"></el-input>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="目标IP" prop="TargetIp">
                            <el-input v-model="form.TargetIp"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="目标端口" prop="TargetPort">
                            <el-input v-model="form.TargetPort"></el-input>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="目标端" prop="Name">
                            <el-select v-model="form.Name" placeholder="选择目标">
                                <el-option v-for="(item,index) in clients" :key="index" :label="item.Name" :value="item.Name">
                                </el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="通信通道" prop="TunnelType">
                            <el-select v-model="form.TunnelType" placeholder="选择通信通道">
                                <el-option v-for="(item,key) in shareData.tunnelTypes" :key="key" :label="item" :value="key">
                                </el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="简单说明" prop="Desc">
                <el-input v-model="form.Desc"></el-input>
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref, toRefs } from '@vue/reactivity';
import { addListen } from '../../../apis/udp-forward'
import { injectShareData } from '../../../states/shareData'
import { injectClients } from '../../../states/clients'
import { inject, watch } from '@vue/runtime-core';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup (props, { emit }) {
        const addListenData = inject('add-listen-data');
        const shareData = injectShareData();
        const clientsState = injectClients();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            form: {
                ID: addListenData.value.ID || 0,
                Port: addListenData.value.Port || 0,
                Name: addListenData.value.Name || 'B客户端',
                TargetIp: addListenData.value.TargetIp || '127.0.0.1',
                TargetPort: addListenData.value.TargetPort || '80',
                Desc: addListenData.value.Desc || '',
                TunnelType: (addListenData.value.TunnelType || 8) + '',
            },
            rules: {
                Port: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                TargetIp: [{ required: true, message: '必填', trigger: 'blur' }],
                TargetPort: [
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

        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;

                const json = JSON.parse(JSON.stringify(state.form));
                json.ID = Number(json.ID);
                json.Port = Number(json.Port);
                json.TargetPort = Number(json.TargetPort);
                json.TunnelType = Number(json.TunnelType);
                addListen(json).then(() => {
                    state.loading = false;
                    state.show = false;
                    emit('success');
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }
        const handleCancel = () => {
            state.show = false;
        }

        return {
            shareData, ...toRefs(state), ...toRefs(clientsState), formDom, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped></style>