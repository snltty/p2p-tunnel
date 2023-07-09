<template>
    <el-dialog append-to-body title="设置到期时间" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="270px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="0px">
            <el-form-item label="" prop="endtime">
                <el-date-picker v-model="state.form.endtime" type="datetime" :clearable="false" />
            </el-form-item>
            <el-form-item label="">
                <div>
                    <el-input size="small" v-model="state.form.addYear" style="width:4rem"></el-input>年
                    <el-input size="small" v-model="state.form.addMonth" style="width:4rem"></el-input>月
                    <el-input size="small" v-model="state.form.addDate" style="width:4rem"></el-input>日
                </div>
                <div class="flex w-100">
                    <div>
                        <el-input size="small" v-model="state.form.addHour" style="width:4rem"></el-input>时
                        <el-input size="small" v-model="state.form.addMinute" style="width:4rem"></el-input>分
                        <el-input size="small" v-model="state.form.addSecond" style="width:4rem"></el-input>秒
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
                endtime: addData.value.EndTime,
                addYear: 0,
                addMonth: 0,
                addDate: 0,
                addHour: 0,
                addMinute: 0,
                addSecond: 0,
            },
            rules: {
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const handleAdd = () => {
            let datetime = new Date(state.form.endtime);
            let y = datetime.getFullYear() + Number(state.form.addYear);
            let m = datetime.getMonth() + Number(state.form.addMonth);
            let d = datetime.getDate() + Number(state.form.addDate);
            let h = datetime.getHours() + Number(state.form.addHour);
            let mm = datetime.getMinutes() + Number(state.form.addMinute);
            let ss = datetime.getSeconds() + Number(state.form.addSecond);

            let date = new Date(y, m, d, h, mm, ss);
            if (isNaN(date.getFullYear()) == false) {
                state.form.endtime = date;
            }

            state.form.addYear = 0;
            state.form.addMonth = 0;
            state.form.addDate = 0;
            state.form.addHour = 0;
            state.form.addMinute = 0;
            state.form.addSecond = 0;
        }

        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }

                let json = JSON.parse(JSON.stringify(addData.value));
                json.EndTime = new Date(state.form.endtime).format('yyyy-MM-dd hh:mm:ss');
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
            state, formDom, handleSubmit, handleCancel, handleAdd
        }
    }
}
</script>
<style lang="stylus" scoped></style>