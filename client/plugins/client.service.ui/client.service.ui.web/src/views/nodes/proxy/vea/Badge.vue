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
                <el-dropdown-item divided class="t-c">
                    <el-button size="small" @click="handleOnlines">在线设备</el-button>
                    <el-dropdown style="height:auto;margin-left:.6rem">
                        <el-button size="small" class="forward-status" :class="`forward-status-${state.data.LastError}`">最后失败
                            <el-icon>
                                <Warning />
                            </el-icon>
                        </el-button>
                        <template #dropdown>
                            <el-dropdown-menu>
                                <template v-for="(item,index) in shareData.commandMsgs" :key="index">
                                    <el-dropdown-item class="forward-success" v-if="state.data.LastError==0 || state.data.LastError > index" :icon="CircleCheck">{{item}}</el-dropdown-item>
                                    <el-dropdown-item class="forward-error" v-else :icon="CircleClose">{{item}}</el-dropdown-item>
                                </template>
                            </el-dropdown-menu>
                        </template>
                    </el-dropdown>

                </el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
    <Onlines :id="state.id" v-if="state.showOnlines" v-model="state.showOnlines"></Onlines>
</template>

<script>
import { websocketState } from '../../../../apis/request'
import { getList, reset, update } from '../../../../apis/vea'
import { shareData } from '../../../../states/shareData'
import { reactive } from '@vue/reactivity'
import { ElMessage } from 'element-plus'
import Onlines from './OnLines.vue'
import { CircleCheck, CircleClose } from '@element-plus/icons'
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['params'],
    components: { Onlines },
    setup(props) {

        const id = props.params.ConnectionId;
        const state = reactive({
            loading: false,
            showOnlines: false,
            id: id,
            data: { IP: '', LanIPs: [] }
        });

        const loadData = () => {
            if (websocketState.connected) {
                update();
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
                ElMessage.success('已更新,请稍后一点时间再次查看列表');
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

        const handleOnlines = () => {
            state.showOnlines = true;
        }

        return {
            CircleCheck, CircleClose, shareData, state, loadData, handleUpdate, handleResetVea, handleOnlines
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

    .forward-status {
        color: red;
    }

    .forward-status-0 {
        color: green;
    }
}
</style>