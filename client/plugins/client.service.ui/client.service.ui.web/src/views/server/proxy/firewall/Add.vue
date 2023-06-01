<template>
    <el-dialog :title="state.form.ID > 0?'编辑':'新增'" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="400px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="60px">
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                        <el-form-item label="类别" prop="Type">
                            <el-select size="default" v-model="state.form.Type">
                                <el-option label="允许" :value="0"></el-option>
                                <el-option label="阻止" :value="1"></el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>
                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                        <el-form-item label="协议" prop="Protocol">
                            <el-select size="default" v-model="state.form.Protocol">
                                <el-option label="TCP" :value="1"></el-option>
                                <el-option label="UDP" :value="2"></el-option>
                                <el-option label="TCP/UDP" :value="3"></el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                        <el-form-item label="端口" prop="Port">
                            <el-input v-model="state.form.Port" placeholder="端口号">
                                <template #append>
                                    <el-tooltip class="box-item" effect="dark" content="英文逗号,间隔表示多个，/间隔表示范围，0代表所有" placement="top">
                                        <el-icon>
                                            <Warning />
                                        </el-icon>
                                    </el-tooltip>
                                </template>
                            </el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                        <el-form-item label="作用域" prop="PluginId">
                            <el-select size="default" v-model="state.form.PluginId">
                                <el-option v-for="item in state.clientProxys" :key="item.value" :label="item.text" :value="item.value" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="IP" prop="IP">
                <el-input type="textarea" v-model="state.form.IP" placeholder="ip，可用掩码，英文逗号或换行间隔" resize="none" :autosize="{minRows:4,maxRows:6}"></el-input>
            </el-form-item>
            <el-form-item label="备注" prop="Remark">
                <el-input v-model="state.form.Remark"></el-input>
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" :loading="state.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { reactive, ref } from '@vue/reactivity';
import { add } from '../../../../apis/proxy-server'
import { shareData } from '../../../../states/shareData'
import { inject, watch, computed } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {
        const addData = inject('add-data');
        const state = reactive({
            show: props.modelValue,
            loading: false,
            clientProxys: computed(() => [{ text: '全局', value: 0xff }].concat(shareData.clientProxys)),
            form: {
                ID: addData.value.ID || 0,
                Port: addData.value.Port || '0',
                Protocol: addData.value.Protocol || 1,
                PluginId: addData.value.PluginId || 0xff,
                Type: addData.value.Type || 0,
                IP: (addData.value.IP || ['0.0.0.0/0']).join('\n'),
                Remark: addData.value.Remark || ''
            },
            rules: {
                Port: [
                    { required: true, message: '必填', trigger: 'blur' }
                ],
                IP: [
                    { required: true, message: '必填', trigger: 'blur' }
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
                json.Port = json.Port.replace(/\s/g, '');
                json.Protocol = +json.Protocol;
                json.Type = +json.Type;
                json.IP = json.IP.splitStr();
                add(json).then((res) => {
                    state.loading = false;
                    if (res) {
                        state.show = false;
                        emit('success');
                        ElMessage.success('操作成功');
                    } else {
                        ElMessage.error('操作失败，存在冲突或格式错误！');
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
            state, formDom, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped></style>