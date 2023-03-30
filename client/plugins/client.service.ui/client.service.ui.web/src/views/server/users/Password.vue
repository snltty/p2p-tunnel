<template>
    <el-dialog title="重置密码" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="270px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="0px">
            <el-form-item label="" prop="password">
                <el-input v-model="state.form.password"></el-input>
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
            form: {
                password: addData.value.Password
            },
            rules: {
                password: [
                    {
                        required: true, min: 6, max: 32, message: '6-32个字符', type: 'string', trigger: 'blur',
                    }
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

                let json = JSON.parse(JSON.stringify(addData.value));
                json.Password = state.form.password;
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
            state, formDom, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped></style>