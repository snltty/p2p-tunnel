<template>
    <div class="servers-mark absolute" :class="{show:state.animation}" @click="handleClose" ref="rootDom">
        <div class="servers-wrap absolute scrollbar" @click.stop>
            <ul>
                <li>
                    <el-button size="small" @click="state.showAdd = true;">添加服务器节点</el-button>
                </li>
                <template v-for="(item,index) in state.servers" :key="index">
                    <li class="flex" @click="handleSelect(item)">
                        <div class="country-img">
                            <img :src="shareData.serverImgs[item.Img].img">
                        </div>
                        <div class="country-name">{{shareData.serverImgs[item.Img].name}}<span v-if="item.Name">-{{item.Name}}</span></div>
                        <div class="flex-1"></div>
                        <div class="country-time">
                            <Signal :value="state.pings[index]"></Signal>
                        </div>
                        <div class="oper">
                            <el-popconfirm title="删除不可逆，是否确认?" @confirm="handleDelete(index)">
                                <template #reference>
                                    <el-button type="danger" icon="Delete" size="small" circle @click.stop />
                                </template>
                            </el-popconfirm>
                        </div>
                    </li>
                </template>
            </ul>
        </div>
        <AddServer v-if="state.showAdd" v-model="state.showAdd" @success="loadData" @click.stop></AddServer>
    </div>
</template>  
   
<script>
import { onMounted, onUnmounted, reactive, ref, watch } from '@vue/runtime-core'
import Signal from '../../components/Signal.vue'
import { getRegisterInfo, sendPing, updateConfig } from '../../apis/register'
import { ElLoading } from 'element-plus'
import { ElMessage } from 'element-plus/lib/components'
import { injectShareData } from '../../states/shareData'
import AddServer from './AddServer.vue'
export default {
    components: { Signal, AddServer },
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {

        const shareData = injectShareData();
        const state = reactive({
            showAdd: false,
            servers: [],
            pings: [],
            animation: false
        })
        watch(() => state.animation, (val) => {
            if (val == false) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const loadData = () => {
            clearTimeout(timer);
            state.servers = [];
            state.pings = [];
            getRegisterInfo().then((info) => {
                state.servers = JSON.parse(JSON.stringify(info.ServerConfig.Items));
                loadPingData(state.servers.map(c => c.Ip));
            });
        }

        let timer = 0;
        const loadPingData = (ips) => {
            sendPing(ips).then((res) => {
                state.pings = res;
                timer = setTimeout(() => {
                    loadPingData(ips);
                }, 1000);
            });
        }
        onMounted(() => {
            loadData();
            setTimeout(() => {
                state.animation = true;
            });
        });
        onUnmounted(() => {
            clearTimeout(timer);
        })

        const handleClose = () => {
            if (loadingInstance != null) return;
            state.animation = false;
            emit('success');
        }
        const handleDelete = (index) => {
            state.servers.splice(index, 1);
            state.pings.splice(index, 1);
            loadingInstance = ElLoading.service({ target: rootDom.value });
            getRegisterInfo().then((json) => {
                json.ServerConfig.Items = JSON.parse(JSON.stringify(state.servers));
                updateConfig(json).then(() => {
                    loadingInstance.close();
                    loadingInstance = null;
                    loadData();
                }).catch(() => {
                    loadingInstance.close();
                    loadingInstance = null;
                    ElMessage.error('更新失败');
                });
            }).catch(() => {
                loadingInstance.close();
            });
        }

        const rootDom = ref(null);
        let loadingInstance = null;
        const handleSelect = (item) => {
            loadingInstance = ElLoading.service({ target: rootDom.value });
            getRegisterInfo().then((json) => {
                json.ServerConfig.Ip = item.Ip;
                json.ServerConfig.UdpPort = item.UdpPort;
                json.ServerConfig.TcpPort = item.TcpPort;
                updateConfig(json).then(() => {
                    loadingInstance.close();
                    loadingInstance = null;
                    handleClose();
                }).catch(() => {
                    loadingInstance.close();
                    loadingInstance = null;
                    ElMessage.error('选择失败');
                });
            }).catch(() => {
                loadingInstance.close();
            });
        }

        return { shareData, state, loadData, handleClose, handleSelect, rootDom, handleDelete }
    }
}
</script>

<style lang="stylus" scoped>
.servers-mark {
    background-color: rgba(0, 0, 0, 0.05);
    overflow: hidden;

    &.show {
        .servers-wrap {
            left: 100%;
            transform: translateX(-20rem);
        }
    }
}

.servers-wrap {
    left: 100%;
    transform: translateX(0);
    right: auto;
    width: 20rem;
    border-left: 1px solid #ddd;
    box-shadow: -1px -1px 0.6rem rgba(0, 0, 0, 0.05);
    background-color: #fff;
    transition: 0.3s cubic-bezier(0.56, -0.37, 0.78, 1.66);

    ul {
        padding: 1rem;
    }
}

li {
    cursor: pointer;
    margin: 0 auto;
    border: 1px solid #ddd;
    width: 100%;
    padding: 0.6rem;
    border-radius: 0.4rem;
    transition: 0.3s;
    box-sizing: border-box;
    margin-bottom: 1rem;
    position: relative;

    &:first-child {
        border: 0;
        text-align: center;
    }

    &:first-child:hover {
        box-shadow: none;
    }

    &:hover {
        box-shadow: 0 0 0 0.4rem #d1d8e261;
    }

    .country-img {
        font-size: 0;
        margin-right: 0.6rem;

        img {
            height: 2rem;
        }
    }

    .country-name {
        line-height: 2rem;
        color: #555;
    }

    .country-time {
        padding-top: 0.2rem;
    }

    .oper {
        position: absolute;
        right: 0.6rem;
        top: 0.4rem;
        display: none;
    }

    &:hover .oper {
        display: block;
    }
}
</style>