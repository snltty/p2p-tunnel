<template>
    <el-dropdown :hide-on-click="false" @visible-change="loadData">
        <span class="el-dropdown-link">
            <a href="javascript:;">组网</a>
            <el-icon>
                <arrow-down />
            </el-icon>
        </span>
        <template #dropdown>
            <el-dropdown-menu>
                <el-dropdown-item>网卡IP : {{state.data.IP}}</el-dropdown-item>
                <template v-for="(item,index) in state.data.LanIPs" :key="index">
                    <template v-if="index == 0">
                        <el-dropdown-item divided>网段{{index+1}} : {{item.IPAddress}}/{{item.Mask}}</el-dropdown-item>
                    </template>
                    <template v-else>
                        <el-dropdown-item>网段{{index+1}} : {{item.IPAddress}}/{{item.Mask}}</el-dropdown-item>
                    </template>
                </template>
                <el-dropdown-item divided>
                    <el-button size="small" :loading="state.loading" @click="handleResetVea">重装网卡</el-button>
                    <el-button size="small" :loading="state.loading" @click.stop="handleUpdate">刷新列表</el-button>
                </el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
</template>

<script>
import { websocketState } from '../../../apis/request'
import { getList, reset, update } from '../../../apis/vea'
import { reactive } from '@vue/reactivity'
import { ElMessage } from 'element-plus'
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['params'],
    setup(props) {

        const id = props.params.ConnectionId;
        const state = reactive({
            loading: false,
            data: { IP: '', LanIPs: [] }
        });

        const loadData = () => {
            if (websocketState.connected) {
                getList().then((res) => {
                    if (res[id]) {
                        state.data = res[id];
                    }
                });
            }
        };
        const handleUpdate = () => {
            state.loading = true;
            update().then(() => {
                state.loading = false;
                ElMessage.success('已更新');
            }).catch(() => {
                state.loading = false;
            });
        }
        const handleResetVea = (row) => {
            state.loading = true;
            reset(id).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('已令其重装网卡，安装过程可能需要一定的时间');
                } else {
                    ElMessage.error('失败');
                }
            }).catch(() => {
                state.loading = false;
                ElMessage.error('失败');
            });
        }

        return {
            state, loadData, handleUpdate, handleResetVea
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-dropdown {
    font-size: 1.3rem;
    height: 1.4rem;
    overflow: hidden;
    padding-top: 1px;

    a {
        color: #666;
        vertical-align: top;
    }

    .el-icon {
        vertical-align: middle;
    }
}
</style>