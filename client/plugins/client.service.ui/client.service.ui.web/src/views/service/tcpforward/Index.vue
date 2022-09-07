<!--
 * @Author: snltty
 * @Date: 2021-08-20 00:47:21
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-18 16:37:33
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\tcpforward\Index.vue
-->
<template>
    <div class="forward-wrap">
        <h3 class="title t-c">{{$route.meta.name}}</h3>
        <div class="inner">
            <div class="head flex">
                <el-button type="primary" size="small" @click="handleAddListen">增加转发监听</el-button>
                <el-button size="small" @click="getData">刷新列表</el-button>
                <span class="flex-1"></span>
                <ConfigureModal className="TcpForwardClientConfigure">
                    <el-button size="small">配置插件</el-button>
                </ConfigureModal>
            </div>
            <div class="content">
                <el-row>
                    <template v-for="(item,index) in list" :key="index">
                        <el-col :xs="12" :sm="8" :md="8" :lg="8" :xl="8">
                            <div class="item">
                                <dl>
                                    <dt class="flex">
                                        <span>{{shareData.aliveTypes[item.AliveType]}}</span>
                                        <span class="flex-1 t-c">0.0.0.0:{{item.Port}}</span>
                                        <span>
                                            <el-switch size="small" @click.stop @change="onListeningChange(item)" v-model="item.Listening" style="margin-top:-6px;"></el-switch>
                                        </span>
                                    </dt>
                                    <dd>{{item.Desc}}</dd>
                                    <dd class="forwards">
                                        <el-collapse>
                                            <el-collapse-item title="转发列表">
                                                <ul>
                                                    <template v-for="(fitem,findex) in item.Forwards" :key="findex">
                                                        <li>
                                                            <p class="flex"><span class="flex-1">访问</span><span>{{fitem.SourceIp}}:{{item.Port}}</span></p>
                                                            <p class="flex"><span class="flex-1">目标</span><span>【{{fitem.Name}}】{{fitem.TargetIp}}:{{fitem.TargetPort}}</span></p>
                                                            <p class="flex"><span class="flex-1">通道</span><span>{{shareData.tunnelTypes[fitem.TunnelType]}}</span></p>
                                                            <p class="t-r">
                                                                <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveForward(item,fitem)">
                                                                    <template #reference>
                                                                        <el-button plain type="danger" size="small">删除</el-button>
                                                                    </template>
                                                                </el-popconfirm>
                                                                <el-button plain size="small" @click="handleEditForward(item,fitem)">编辑</el-button>
                                                            </p>
                                                        </li>
                                                    </template>
                                                </ul>
                                            </el-collapse-item>
                                        </el-collapse>
                                    </dd>
                                    <dd class="btns t-r">
                                        <el-popconfirm title="删除不可逆，是否确认" @confirm="handleRemoveListen(item)">
                                            <template #reference>
                                                <el-button plain type="danger" size="small">删除</el-button>
                                            </template>
                                        </el-popconfirm>
                                        <el-button plain type="info" size="small" @click="handleEditListen(item)">编辑</el-button>
                                        <el-button plain type="info" v-if="item.AliveType == 2 || item.Forwards.length < 1" size="small" @click="handleAddForward(item)">增加转发</el-button>
                                    </dd>
                                </dl>
                            </div>
                        </el-col>
                    </template>
                </el-row>
            </div>
            <el-alert class="alert" type="warning" show-icon :closable="false" title="转发" description="转发用于访问不同的地址，127.0.0.1:8000->127.0.0.1:80，A客户端访问127.0.0.1:8000 得到 B客户端的127.0.0.1:80数据，不适用于ftp双通道" />
            <AddForward v-if="showAddForward" v-model="showAddForward" @success="getData"></AddForward>
            <AddListen v-if="showAddListen" v-model="showAddListen" @success="getData"></AddListen>
        </div>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getList, removeListen, startListen, stopListen, removeForward } from '../../../apis/tcp-forward'
import ConfigureModal from '../configure/ConfigureModal.vue'
import { onMounted, provide } from '@vue/runtime-core'
import AddForward from './AddForward.vue'
import AddListen from './AddListen.vue'
import { injectShareData } from '../../../states/shareData'
export default {
    components: { ConfigureModal, AddListen, AddForward },
    setup () {


        const shareData = injectShareData();
        const state = reactive({
            loading: false,
            list: [],
            currentLsiten: { Port: 0 },
            showAddListen: false,
            showAddForward: false,
        });

        const expandKeys = ref([]);
        const getData = () => {
            getList().then((res) => {
                state.list = res;
            });
        };
        const onExpand = (a, b) => {
            expandKeys.value = b.map(c => c.ID);
        }

        const addListenData = ref({ ID: 0 });
        provide('add-listen-data', addListenData);
        const handleAddListen = () => {
            addListenData.value = { ID: 0 };
            state.showAddListen = true;
        }
        const handleEditListen = (row) => {
            addListenData.value = row;
            state.showAddListen = true;
        }

        const handleRemoveListen = (row) => {
            removeListen(row.ID).then(() => {
                getData();
            });
        }
        const onListeningChange = (row) => {
            if (!row.Listening) {
                stopListen(row.ID).then(getData).catch(getData);
            } else {
                startListen(row.ID).then(getData).catch(getData);
            }
        }

        const addForwardData = ref({ forward: { ID: 0 }, ListenID: 0, currentLsiten: { ID: 0, Port: 0 } });
        provide('add-forward-data', addForwardData);

        const handleAddForward = (row) => {
            addForwardData.value.currentLsiten = row;
            addForwardData.value.forward = { ID: 0 };
            state.showAddForward = true;
        }
        const handleEditForward = (listen, forward) => {
            addForwardData.value.currentLsiten = listen;
            addForwardData.value.forward = forward;
            state.showAddForward = true;
        }
        const handleRemoveForward = (listen, forward) => {
            removeForward(listen.ID, forward.ID).then(() => {
                getData();
            });
        }

        onMounted(() => {
            getData();
        });

        return {
            ...toRefs(state), shareData, getData, expandKeys, onExpand,
            handleRemoveListen, handleAddListen, handleEditListen, onListeningChange,
            handleAddForward, handleEditForward, handleRemoveForward,
        }
    }
}
</script>
<style lang="stylus">
.forward-wrap
    .el-collapse-item__header, .el-collapse-item__content, .el-collapse-item__wrap
        border-right: 0;
        border-left: 0;

    .el-collapse-item__content
        padding: 0;
</style>
<style lang="stylus" scoped>
@media screen and (max-width: 500px)
    .el-col-24
        max-width: 100%;
        flex: 0 0 100%;

.forward-wrap
    padding: 2rem;

    .inner
        border: 1px solid #eee;
        // padding: 1rem;
        border-radius: 0.4rem;

    .head
        padding: 1rem;
        border-bottom: 1px solid #eee;

    .content
        padding: 1rem;

        .item
            padding: 1rem 0.6rem;

            dl
                border: 1px solid #eee;
                border-radius: 0.4rem;

                dt
                    border-bottom: 1px solid #eee;
                    padding: 1rem;
                    font-size: 1.4rem;
                    font-weight: 600;
                    color: #555;
                    line-height: 2.4rem;

                dd
                    padding: 0.4rem 1rem;

                    &:nth-child(2)
                        padding: 1rem;
                        background-color: #fafafa;

                    &.forwards
                        padding: 0;

                        li
                            border-bottom: 1px solid #eee;
                            padding: 1rem;

                            &:last-child
                                border: 0;

    .alert
        margin-top: 1rem;
</style>