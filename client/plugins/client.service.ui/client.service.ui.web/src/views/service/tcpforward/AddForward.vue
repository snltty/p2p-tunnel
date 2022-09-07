<!--
 * @Author: snltty
 * @Date: 2022-03-24 15:15:31
 * @LastEditors: snltty
 * @LastEditTime: 2022-05-28 17:33:46
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\tcpforward\AddForward.vue
-->
<template>
    <el-dialog title="转发" destroy-on-close v-model="show" center :close-on-click-modal="false" width="600px">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="80px">
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="源host" prop="SourceIp">
                            <el-input :disabled="addForwardData.currentLsiten.AliveType == 1" v-model="form.SourceIp"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="目标端" prop="Name">
                            <el-select v-model="form.Name" placeholder="选择目标">
                                <el-option v-for="(item,index) in clients" :key="index" :label="item.Name" :value="item.Name">
                                </el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="目标ip" prop="TargetIp">
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
                        <el-form-item label="通信通道" prop="TunnelType">
                            <el-select v-model="form.TunnelType" placeholder="选择通信通道">
                                <el-option v-for="(item,key) in shareData.tunnelTypes" :key="key" :label="item" :value="key">
                                </el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="简单说明" prop="Desc">
                            <el-input v-model="form.Desc"></el-input>
                        </el-form-item>
                    </el-col>
                </el-row>
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
import { addForward } from '../../../apis/tcp-forward'
import { injectClients } from '../../../states/clients'
import { injectShareData } from '../../../states/shareData'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup (props, { emit }) {
        const defaultForm = {
            ListenID: 0,
            ID: 0,
            SourceIp: '0.0.0.0',
            Name: 'B客户端', TargetIp: '127.0.0.1', TargetPort: 80,
            AliveType: '1',
            Desc: '',
            TunnelType: '8'
        };
        const clientsState = injectClients();
        const addForwardData = inject('add-forward-data');
        const shareData = injectShareData();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            form: {
                ID: addForwardData.value.forward.ID || defaultForm.ID,
                SourceIp: addForwardData.value.forward.SourceIp || defaultForm.SourceIp,
                Name: addForwardData.value.forward.Name || defaultForm.Name,
                TargetIp: addForwardData.value.forward.TargetIp || defaultForm.TargetIp,
                TargetPort: addForwardData.value.forward.TargetPort || defaultForm.TargetPort,
                Desc: addForwardData.value.forward.Desc || defaultForm.Desc,
                TunnelType: (addForwardData.value.forward.TunnelType || defaultForm.TunnelType) + '',
            },
            rules: {
                SourceIp: [{ required: true, message: '必填', trigger: 'blur' }],
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

        const remark = computed(() => {
            return [
                `本客户端(${state.form.SourceIp}:${addForwardData.value.currentLsiten.Port})`,
                `<br/>`,
                ` -> `,
                `【${shareData.aliveTypes[addForwardData.value.currentLsiten.AliveType]}】`,
                ` -> `,
                `<br/>`,
                `${state.form.Name}(${state.form.TargetIp}:${state.form.TargetPort})`
            ].join('');
        });

        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;

                state.form.TargetPort = Number(state.form.TargetPort);
                state.form.TunnelType = Number(state.form.TunnelType);
                const json = {
                    ListenID: addForwardData.value.currentLsiten.ID,
                    Forward: state.form
                }
                console.log(json)
                addForward(json).then(() => {
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
            shareData, ...toRefs(state), ...toRefs(clientsState), addForwardData, formDom, remark, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped>
.remark
    margin-top: 1rem;
</style>