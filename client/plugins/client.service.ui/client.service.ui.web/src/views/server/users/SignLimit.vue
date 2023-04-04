<template>
    <el-dialog title="设置登录限制" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="300px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="60">
            <el-form-item label="" label-width="0">
                <el-switch v-model="state.type" active-text="限制登录数" inactive-text="无限制" />
            </el-form-item>
            <el-form-item label="数量" prop="signLimit" v-if="state.type">
                <el-input v-model="state.form.signLimit" />
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
import { inject, watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {
        const addData = inject('add-data');


        const state = reactive({
            show: props.modelValue,
            loading: false,
            type: addData.value.SignLimit != 0,
            form: {
                signLimit: addData.value.SignLimit,
            },
            rules: {
                signLimit: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 0, max: 65535, message: '数字 0-65535', trigger: 'blur', transform(value) {
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

                let json = JSON.parse(JSON.stringify(addData.value));
                json.SignLimit = state.type ? +state.form.signLimit : 0;
                state.loading = true;
                console.log(json);
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
            state, formDom, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped></style>