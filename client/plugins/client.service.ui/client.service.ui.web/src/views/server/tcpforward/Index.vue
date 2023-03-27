<template>
    <div class="forward-wrap">
        <div class="inner">
            <div class="head flex">
                <el-button type="primary" size="small" @click="handleAddListen">增加tcp长连接端口</el-button>
                <el-button size="small" @click="loadPorts">刷新列表</el-button>
                <span class="flex-1"></span>
            </div>
            <div class="content">
                <el-row>
                    <template v-for="(item,index) in state.list" :key="index">
                        <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                            <div class="item">
                                <dl>
                                    <dt class="flex">
                                        <span>{{shareData.aliveTypes[item.AliveType]}}</span>
                                        <span class="flex-1 t-c">{{item.Domain}}:{{item.ServerPort}}</span>
                                        <span v-if="item.AliveType == shareData.aliveTypesName.tunnel">
                                            <el-switch size="small" @click.stop @change="onListeningChange(item,item.Forwards[0])" v-model="item.Forwards[0].Listening" style="margin-top:-6px;"></el-switch>
                                        </span>
                                    </dt>
                                    <dd>{{item.Desc}}</dd>
                                    <dd class="forwards">
                                        <el-collapse>
                                            <el-collapse-item title="转发列表">
                                                <ul>
                                                    <template v-for="(fitem,findex) in item.Forwards" :key="findex">
                                                        <li>
                                                            <p class="flex"><span class="flex-1">访问</span><span>{{fitem.sourceText}}</span></p>
                                                            <p class="flex"><span class="flex-1">目标</span><span>【本机】{{fitem.distText}}</span></p>
                                                            <p class="t-r">
                                                                <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(item,fitem)">
                                                                    <template #reference>
                                                                        <el-button plain type="danger" v-if="item.AliveType == shareData.aliveTypesName.web" size="small">删除</el-button>
                                                                    </template>
                                                                </el-popconfirm>
                                                            </p>
                                                        </li>
                                                    </template>
                                                </ul>
                                            </el-collapse-item>
                                        </el-collapse>
                                    </dd>
                                    <dd class="btns t-r">
                                        <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(item,item.Forwards[0])">
                                            <template #reference>
                                                <el-button v-if="item.AliveType == shareData.aliveTypesName.tunnel" plain type="danger" size="small">删除</el-button>
                                            </template>
                                        </el-popconfirm>
                                        <el-button plain type="info" v-if="item.AliveType == shareData.aliveTypesName.web" size="small" @click="handleAddForward(item)">增加转发</el-button>
                                    </dd>
                                </dl>
                            </div>
                        </el-col>
                    </template>
                </el-row>
            </div>
        </div>
        <AddForward v-if="state.showAddForward" v-model="state.showAddForward" @success="loadPorts"></AddForward>
        <AddListen v-if="state.showAddListen" v-model="state.showAddListen" @success="loadPorts"></AddListen>
    </div>
</template>

<script>
import { onMounted, provide, reactive, ref } from '@vue/runtime-core';
import { getServerPorts, getServerForwards, startServerForward, stopServerForward, removeServerForward } from '../../../apis/tcp-forward-server'
import { injectShareData } from '../../../states/shareData'
import { injectSignIn } from '../../../states/signin'
import AddForward from './AddForward.vue'
import AddListen from './AddListen.vue'
export default {
    service: 'ServerTcpForwardClientService',
    components: { AddForward, AddListen },
    setup() {

        const shareData = injectShareData();
        const signinState = injectSignIn();
        const state = reactive({
            loading: false,
            list: [],
            showAddForward: false,
            showAddListen: false
        });

        const expandKeys = ref([]);
        const onExpand = (a, b) => {
            expandKeys.value = b.map(c => c.ServerPort);
        }

        const loadPorts = () => {
            getServerPorts().then((res) => {
                loadForwards(res);
            });
        }
        const loadForwards = (ports) => {
            getServerForwards().then((forwards) => {
                state.list = ports.splice(0, ports.length - 2).map(c => {
                    return {
                        ServerPort: c,
                        Domain: signinState.ServerConfig.Ip,
                        Desc: '短链接',
                        AliveType: shareData.aliveTypesName.web,
                        Forwards: forwards.filter(d => d.AliveType == shareData.aliveTypesName.web && d.ServerPort == c).map(d => {
                            return {
                                Domain: d.Domain,
                                Listening: d.Listening,
                                Desc: d.Desc,
                                LocalIp: d.LocalIp,
                                LocalPort: d.LocalPort,
                                sourceText: `${d.Domain}:${d.ServerPort}`,
                                distText: `${d.LocalIp}:${d.LocalPort}`
                            }
                        })
                    }
                }).concat(forwards.filter(c => c.AliveType == shareData.aliveTypesName.tunnel).map(d => {
                    return {
                        ServerPort: d.ServerPort,
                        Domain: signinState.ServerConfig.Ip,
                        Desc: d.Desc || '长连接',
                        AliveType: shareData.aliveTypesName.tunnel,
                        Listening: d.Listening,
                        Forwards: [
                            {
                                Domain: d.Domain,
                                Listening: d.Listening,
                                Desc: d.Desc,
                                LocalIp: d.LocalIp,
                                LocalPort: d.LocalPort,
                                sourceText: `${signinState.ServerConfig.Ip}:${d.ServerPort}`,
                                distText: `${d.LocalIp}:${d.LocalPort}`
                            }
                        ]
                    }
                }));
            })
        }

        const onListeningChange = (listen, forward) => {
            let func = !forward.Listening ? stopServerForward : startServerForward;
            state.loading = true;
            func({
                AliveType: listen.AliveType,
                Domain: forward.Domain,
                ServerPort: listen.ServerPort,
                LocalIp: forward.LocalIp,
                LocalPort: forward.LocalPort
            }).then((res) => {
                state.loading = false;
                if (res) {
                    loadPorts();
                }
            }).catch(() => {
                state.loading = false;
            })
        }
        const handleRemoveListen = (listen, forward) => {
            state.loading = true;
            removeServerForward({
                AliveType: listen.AliveType,
                Domain: forward.Domain,
                ServerPort: listen.ServerPort,
                LocalIp: forward.LocalIp,
                LocalPort: forward.LocalPort
            }).then((res) => {
                state.loading = false;
                if (res) {
                    loadPorts();
                }
            }).catch(() => {
                state.loading = false;
            })
        }

        const addForwardData = ref({ AliveType: shareData.aliveTypesName.web, ServerPort: 0 });
        provide('add-forward-data', addForwardData);
        const handleAddForward = (row) => {
            addForwardData.value.ServerPort = row.ServerPort;
            state.showAddForward = true;
        }

        const addListenData = ref({ AliveType: shareData.aliveTypesName.tunnel });
        provide('add-listen-data', addListenData);
        const handleAddListen = () => {
            state.showAddListen = true;
        }
        onMounted(() => {
            loadPorts();
        });

        return {
            state, shareData, loadPorts, onExpand, expandKeys,
            handleRemoveListen, handleAddForward, handleAddListen, onListeningChange
        }
    }
}
</script>

<style lang="stylus">
.forward-wrap {
    .el-collapse-item__header, .el-collapse-item__content, .el-collapse-item__wrap {
        border-right: 0;
        border-left: 0;
    }

    .el-collapse-item__content {
        padding: 0;
    }
}
</style>
<style lang="stylus" scoped>
@media screen and (max-width: 500px) {
    .el-col-24 {
        max-width: 100%;
        flex: 0 0 100%;
    }
}

.forward-wrap {
    padding: 2rem;

    .inner {
        border: 1px solid var(--main-border-color);
        // padding: 1rem;
        border-radius: 0.4rem;
    }

    .head {
        padding: 1rem;
        border-bottom: 1px solid var(--main-border-color);
    }

    .content {
        padding: 1rem;

        .item {
            padding: 1rem 0.6rem;

            dl {
                border: 1px solid var(--main-border-color);
                border-radius: 0.4rem;

                dt {
                    border-bottom: 1px solid var(--main-border-color);
                    padding: 1rem;
                    font-size: 1.4rem;
                    font-weight: 600;
                    color: #555;
                    line-height: 2.4rem;
                }

                dd {
                    padding: 0.4rem 1rem;

                    &:nth-child(2) {
                        padding: 1rem;
                        background-color: #fafafa;
                    }

                    &.forwards {
                        padding: 0;

                        li {
                            border-bottom: 1px solid #eee;
                            padding: 1rem;

                            &:last-child {
                                border: 0;
                            }
                        }
                    }
                }
            }
        }
    }

    .alert {
        margin-top: 1rem;
    }
}
</style>