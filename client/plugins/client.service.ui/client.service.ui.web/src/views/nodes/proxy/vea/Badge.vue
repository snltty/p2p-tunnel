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
                <el-dropdown-item class="flex">
                    <span>网卡IP : {{state.data.IP}}</span>
                    <span class="flex-1"></span>
                    <el-button size="small" @click.stop="handleTest(state.data.IP,32)">测试</el-button></el-dropdown-item>
                <template v-for="(item,index) in state.data.LanIPs" :key="index">
                    <el-dropdown-item>
                        <span>网段{{index+1}} : {{item.IPAddress}}/{{item.Mask}}</span>
                        <span class="flex-1"></span>
                        <el-button size="small" @click.stop="handleTest(item.IPAddress,item.Mask)">测试</el-button></el-dropdown-item>
                </template>
                <el-dropdown-item divided>
                    <el-button size="small" :loading="state.loading" @click="handleResetVea">重装网卡</el-button>
                    <el-button size="small" :loading="state.loading" @click.stop="handleUpdate">刷新列表</el-button>
                    <el-button size="small" @click="handleOnlines">在线设备</el-button>
                </el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
    <Onlines :id="state.id" v-if="state.showOnlines" v-model="state.showOnlines"></Onlines>
    <StatusMsg v-if="state.showStatusMsg" v-model="state.showStatusMsg" :msgCallback="state.statusMsgCallback"></StatusMsg>
</template>

<script>
import { websocketState } from '../../../../apis/request'
import { getList, reset, update, test } from '../../../../apis/vea'
import { shareData } from '../../../../states/shareData'
import { reactive } from '@vue/reactivity'
import { ElMessageBox } from 'element-plus'
import Onlines from './OnLines.vue'
import StatusMsg from '../../../../components/StatusMsg.vue'
import { CircleCheck, CircleClose } from '@element-plus/icons'
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['params'],
    components: { Onlines, StatusMsg },
    setup(props) {

        const id = props.params.ConnectionId;
        const state = reactive({
            loading: false,
            showOnlines: false,
            id: id,
            data: { IP: '', LanIPs: [] },
            showStatusMsg: false,
            statusMsgCallback: () => 0
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
        const handleTest = (ip, mask) => {
            const maskValue = mask.maskLength2value();
            const ipValue = ip.ipv42number();
            const network = ipValue.ipv42network(maskValue);
            const broadcast = ipValue.ipv42broadcast(maskValue);
            //第一个可用ip
            const firstIp = ipValue == network && ipValue == broadcast ? ipValue.toIpv4Str() : (network + 1).toIpv4Str();

            ElMessageBox.prompt('不带http://，带端口', '测试', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputValue: `${firstIp}:80`
            }).then(({ value }) => {
                let arr = value.split(':');
                state.statusMsgCallback = test(arr[0], +arr[1]);
                state.showStatusMsg = true;
            }).catch(() => {
            });
        }

        return {
            CircleCheck, CircleClose, shareData, state, loadData, handleUpdate, handleResetVea, handleOnlines, handleTest
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