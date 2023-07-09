<template>
    <div>
        <a href="javascript:;" @click="state.dialogVisible = true;">速度</a>
        <el-dialog append-to-body v-model="state.dialogVisible">
            <div class="t-c">
                <span>包大小:</span> <el-input v-model="state.packet" style="width:6rem"></el-input>KB
                <template v-if="state.loading">
                    <el-button type="error" @click="handleStop">停止</el-button>
                </template>
                <template v-else>
                    <el-button :loading="state.loading" @click="handleStart">开始</el-button>
                </template>
            </div>
            <div class="t-c" style="margin-top:2rem">
                <span>{{state.speed}}MB/s</span>
            </div>
        </el-dialog>
    </div>
</template>

<script>
import { sendTest } from '../../../apis/clients'
import { subNotifyMsg, unsubNotifyMsg } from '../../../apis/request'
import { reactive } from '@vue/reactivity'
import { onMounted, onUnmounted, watch } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['params'],
    setup(props) {
        const id = props.params.ConnectionId;
        const state = reactive({
            packet: 32,
            loading: false,
            speed: 0,
            dialogVisible: false
        });
        watch(() => state.dialogVisible, (value) => {
            if (!value) {
                handleStop();
            }
        });

        const onMessage = (msg) => {
            state.speed = ((msg.current - msg.prev) / 1024 / 1024).toFixed(2);
            state.loading = true;
        };
        onMounted(() => {
            subNotifyMsg('speed_test', onMessage);
        });
        onUnmounted(() => {
            unsubNotifyMsg('speed_test', onMessage);
            handleStop();
        })

        const handleStart = () => {
            state.dialogVisible = true;
            sendTest({
                id: id,
                packet: +state.packet
            });
        }
        const handleStop = () => {
            sendTest({ id: 0, packet: 0 });
            state.loading = false;
        }

        return {
            state, handleStart, handleStop
        }
    }
}
</script>

<style lang="stylus" scoped></style>