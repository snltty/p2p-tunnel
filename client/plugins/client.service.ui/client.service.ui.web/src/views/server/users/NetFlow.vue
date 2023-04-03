<template>
    <el-dialog title="设置流量" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="300px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="90">
            <el-form-item label="" label-width="0">
                <el-switch v-model="state.type" active-text="限制流量" inactive-text="无限流量" />
            </el-form-item>
            <el-form-item label="流量(byte)" prop="netflow" v-if="state.type">
                <div>
                    <el-input v-model="state.form.netflow" />
                </div>
                <div>
                    {{format}}
                </div>
            </el-form-item>
            <el-form-item label="" label-width="0" v-if="state.type">
                <div class="flex w-100">
                    <div>
                        <el-input size="small" v-model="state.form.addTB" style="width:4rem"></el-input>TB
                        <el-input size="small" v-model="state.form.addGB" style="width:4rem"></el-input>GB
                        <el-input size="small" v-model="state.form.addMB" style="width:4rem"></el-input>MB
                    </div>
                    <div class="flex-1"></div>
                    <div>
                        <el-button size="small" @click="handleAdd">增加</el-button>
                    </div>
                </div>
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
import { add } from '../../../apis/users-server'
import { computed, inject, watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {
        const addData = inject('add-data');


        const state = reactive({
            show: props.modelValue,
            loading: false,
            type: addData.value.NetFlow != -1,
            form: {
                netflow: addData.value.NetFlow,
                addTB: 0,
                addGB: 0,
                addMB: 0
            },
            rules: {
                netflow: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: -1, max: 9223372036854775807, message: '数字 -1-9223372036854775807', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
            }
        });
        const format = computed(() => Number(state.form.netflow).sizeFormat().join(''));

        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const handleAdd = () => {
            let netFlow = Number(state.form.netflow)
                + Number(state.form.addTB) * 1024 * 1024 * 1024 * 1024
                + Number(state.form.addGB) * 1024 * 1024 * 1024
                + Number(state.form.addMB) * 1024 * 1024;

            if (isNaN(netFlow) == false) {
                state.form.netflow = netFlow;
            }
            state.form.addTB = 0;
            state.form.addGB = 0;
            state.form.addMB = 0;
        }

        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }

                let json = JSON.parse(JSON.stringify(addData.value));
                json.NetFlow = state.type ? +state.form.netflow : -1;
                state.loading = true;
                add(json).then((msg) => {
                    state.loading = false;
                    if (!msg) {
                        ElMessage.success('操作成功');
                        emit('success');
                        state.show = false;
                    } else {
                        ElMessage.error(msg);
                    }
                }).catch(() => {
                    ElMessage.error('操作失败');
                    state.loading = false;
                });
            })
        }
        const handleCancel = () => {
            state.show = false;
        }

        return {
            format, state, formDom, handleSubmit, handleCancel, handleAdd
        }
    }
}
</script>
<style lang="stylus" scoped></style>