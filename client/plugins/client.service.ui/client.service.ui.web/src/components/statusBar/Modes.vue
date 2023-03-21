<template>
    <div class="modes-wrap">
        <el-dropdown size="small" title="不同的模式，可能会隐藏部分功能，及自动修改部分配置" @command="handleCommand">
            <span class="el-dropdown-link">
                <span>{{name}}</span>
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
import { reactive, computed } from '@vue/reactivity'
import { getSignInInfo, updateConfig } from '../../apis/signin'
import { injectServices } from '../../states/services'
import { ElMessage } from 'element-plus/lib/components';
import { ElLoading, ElMessageBox } from 'element-plus';
export default {
    setup() {

        const services = ["LoggerClientService", "HttpProxyClientService",
            "ServerTcpForwardClientService", "TcpForwardClientService",
            "ServerUdpForwardClientService", "UdpForwardClientService",
            "ClientsClientService", "ConfigureClientService",
            "CounterClientService", "SignInClientService",
            "Socks5ClientService", "VeaClientService",
            "WakeUpClientService"];

        const files = require.context('../../views/', true, /mode\.js/);
        const funcs = files.keys().map(c => files(c));

        const servicesState = injectServices();
        const name = computed(() => {
            let name = servicesState.services[0] || 'full';
            let _modes = state.modes.filter(c => c.name == name);
            if (_modes.length > 0) {
                return _modes[0].text;
            }
            return state.modes[0].text;
        });
        const state = reactive({
            modes: [
                { name: 'full', text: '完全功能', services: [] },
                {
                    name: 'p2p', text: '仅打洞穿透', services: [
                        'full', 'LoggerClientService', 'ConfigureClientService', 'SignInClientService', 'ClientsClientService',
                        'HttpProxyClientService', 'TcpForwardClientService', 'UdpForwardClientService',
                        'Socks5ClientService', 'VeaClientService', 'WakeUpClientService'
                    ]
                },
                {
                    name: 'rproxy', text: '仅代理穿透', services: [
                        'rproxy', 'LoggerClientService', 'ConfigureClientService', 'SignInClientService',
                        'ServerTcpForwardClientService', 'ServerUdpForwardClientService'
                    ]
                },
                {
                    name: 'proxy', text: '仅代理翻越', services: [
                        'proxy', 'LoggerClientService', 'ConfigureClientService', 'SignInClientService',
                        'HttpProxyClientService', 'Socks5ClientService'
                    ]
                }
            ]
        });

        let loadingInstance = null;
        const _updateConfig = (funcs, command) => {
            return new Promise((resolve, reject) => {
                const fn = (index = 0) => {
                    if (index >= funcs.length) {
                        resolve();
                        return;
                    }
                    funcs[index].update(command.services, command.name).then(() => {
                        fn(++index);
                    }).catch(reject);
                }
                fn();
            });
        }
        const handleCommand = (command) => {
            let remarks = funcs.reduce((value, item) => value.concat(item.remarks(command.services, command.name)), []);
            ElMessageBox.confirm(remarks.join('</br>'), '提示', { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消', dangerouslyUseHTMLString: true })
                .then(() => {
                    loadingInstance = ElLoading.service({ target: '.wrap' });
                    _updateConfig(funcs, command).then(() => {
                        getSignInInfo().then((json) => {
                            json.ClientConfig.Services = command.services;
                            updateConfig(json).then(() => {
                                loadingInstance.close()
                                ElMessage.success('成功，刷新生效');
                            }).catch((e) => {
                                console.log(e);
                                ElMessage.error('失败' + e);
                                loadingInstance.close();
                            });
                        }).catch((e) => {
                            ElMessage.error('失败' + e);
                            loadingInstance.close();
                        });
                    }).catch((e) => {
                        ElMessage.error('失败' + e);
                        loadingInstance.close();
                    });
                }).catch(() => {
                });
        }

        return {
            name, state, handleCommand
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