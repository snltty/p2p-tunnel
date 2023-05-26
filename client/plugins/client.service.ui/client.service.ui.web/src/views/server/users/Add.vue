<template>
    <el-dialog title="增加账号" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="300px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="60px">
            <el-form-item label="账号" prop="Account">
                <el-input v-model="state.form.Account"></el-input>
            </el-form-item>
            <el-form-item label="密码" prop="Password">
                <el-input v-model="state.form.Password"></el-input>
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
import { watch } from '@vue/runtime-core';
import { ElMessage } from 'element-plus';
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {
        const state = reactive({
            show: props.modelValue,
            loading: false,
            form: {
                ID: 0,
                Account: '',
                Password: '',
                Access: 0,
                SignLimit: 0,
                NetFlow: 0,
                EndTime: new Date().format('yyyy-MM-dd hh:mm:ss'),
            },
            rules: {
                Account: [
                    { required: true, message: '必填', min: 1, max: 32, message: '1-32个字符', trigger: 'blur' }
                ],
                Password: [
                    { required: true, message: '必填', min: 6, max: 32, message: '6-32个字符', trigger: 'blur' }
                ]
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
                add(json).then((msg) => {
                    state.loading = false;
                    if (!msg) {
                        state.show = false;
                        emit('success');
                    } else {
                        ElMessage.error(msg);
                    }
                }).catch((e) => {
                    state.loading = false;
                    ElMessage.error('操作失败');
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