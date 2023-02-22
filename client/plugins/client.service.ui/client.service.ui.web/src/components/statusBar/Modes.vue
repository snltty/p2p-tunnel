<template>
    <div class="modes-wrap">
        <el-dropdown size="small" title="不同的模式，可能会隐藏部分功能，及自动修改部分配置" @command="handleCommand">
            <span class="el-dropdown-link">
                <span>完全模式</span>
                <el-icon class="el-icon--right">
                    <arrow-down />
                </el-icon>
            </span>
            <template #dropdown>
                <el-dropdown-menu>
                    <template v-for="(item,index) in state.modes" :key="index">
                        <el-dropdown-item :command="item">{{item.text}}</el-dropdown-item>
                    </template>
                </el-dropdown-menu>
            </template>
        </el-dropdown>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import { getRegisterInfo, updateConfig } from '../../apis/register'
import { ElMessage } from 'element-plus/lib/components';
import { ElLoading } from 'element-plus';
export default {
    setup() {

        const services = ["LoggerClientService", "HttpProxyClientService",
            "ServerTcpForwardClientService", "TcpForwardClientService",
            "ServerUdpForwardClientService", "UdpForwardClientService",
            "ClientsClientService", "ConfigureClientService",
            "CounterClientService", "RegisterClientService",
            "Socks5ClientService", "VeaClientService",
            "WakeUpClientService"];

        const files = require.context('../../views/', true, /mode\.js/);
        const func = files.keys().map(c => files(c).default);

        const state = reactive({
            modes: [
                { name: 'full', text: '完全功能', services: [] },
                {
                    name: 'p2p', text: '仅打洞穿透', services: [
                        'LoggerClientService', 'ConfigureClientService', 'RegisterClientService', 'ClientsClientService',
                        'HttpProxyClientService', 'TcpForwardClientService', 'UdpForwardClientService',
                        'Socks5ClientService', 'VeaClientService', 'WakeUpClientService'
                    ]
                },
                {
                    name: 'rproxy', text: '仅代理穿透', services: [
                        'LoggerClientService', 'ConfigureClientService', 'RegisterClientService',
                        'ServerTcpForwardClientService', 'ServerUdpForwardClientService'
                    ]
                },
                {
                    name: 'proxy', text: '仅代理翻越', services: [
                        'LoggerClientService', 'ConfigureClientService', 'RegisterClientService',
                        'HttpProxyClientService', 'Socks5ClientService'
                    ]
                }
            ]
        });
        let loadingInstance = null;
        const handleCommand = (command) => {
            loadingInstance = ElLoading.service({ target: '.wrap' });
            getRegisterInfo().then((json) => {
                json.ClientConfig.Services = command.services;
                updateConfig(json).then(() => {
                    loadingInstance.close()
                    ElMessage.success('成功，刷新生效');
                }).catch(() => {
                    ElMessage.error('失败');
                    loadingInstance.close();
                });
            }).catch(() => {
                ElMessage.error('失败');
                loadingInstance.close();
            });
        }


        return {
            state, handleCommand
        }
    }
}
</script>

<style lang="stylus" scoped>
.modes-wrap {
    padding-top: 0.7rem;
    line-height: normal;

    .el-dropdown-link {
        font-size: 1.2rem;
    }

    .el-icon {
        vertical-align: middle;
    }
}
</style>