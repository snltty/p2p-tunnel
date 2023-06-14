<template>
    <el-dialog title="代理通顺情况" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="260px">
        <div v-loading="state.loading">
            <el-steps direction="vertical" :active="msg">
                <template v-for="(item,index) in shareData.commandMsgs" :key="index">
                    <el-step :title="item" />
                </template>
            </el-steps>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" @click="handleCancel">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, onMounted, reactive, watch } from 'vue';
import { shareData } from '../states/shareData'
export default {
    props: ['modelValue', 'msgCallback'],
    emits: ['update:modelValue'],
    setup(props, { emit }) {

        const state = reactive({
            show: props.modelValue,
            loading: true,
            msg: 0
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const handleCancel = () => {
            state.show = false;
        }
        const msg = computed(() => state.msg == 0 ? shareData.commandMsgs.length : state.msg - 1);

        onMounted(() => {
            props.msgCallback.then((msg) => {
                state.loading = false;
                state.msg = msg;
            }).catch(() => {
                state.loading = false;
            })
        })

        return {
            shareData, state, msg, handleCancel
        }
    }
}
</script>

<style lang="stylus" scoped></style>