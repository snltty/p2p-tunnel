<template>
    <div class="absolute flex">
        <div class="menu h-100">
            <LeftMenu :menus="leftMenus" v-model="state.currentMenu" @handle="handleJumpScroll"></LeftMenu>
        </div>
        <div class="content h-100 flex-1 flex flex-column">
            <div class="inner flex-1 scrollbar" ref="contentDom">
                <template v-for="(item,index) in state.menus" :key="index">
                    <el-divider content-position="left" border-style="dotted">{{item.text}}</el-divider>
                    <div class="setting-item">
                        <component :is="item.component" :ref="`setting_item_${item.text}`" />
                    </div>
                </template>
            </div>
            <div class="btn">
                <el-button type="primary" size="default" @click="handleSave" :loading="state.loading">应用更改</el-button>
            </div>
        </div>
    </div>
</template> 

<script>
import Client from './Client.vue'
import Encode from './Encode.vue'
import TcpForward from './TcpForward.vue'
import UdpForward from './UdpForward.vue'
import HttpProxy from './HttpProxy.vue'
import Socks5 from './Socks5.vue'
import Vea from './Vea.vue'
import { getCurrentInstance, nextTick, onBeforeUnmount, onMounted, reactive, ref, shallowRef } from '@vue/runtime-core'
import { ElMessage } from 'element-plus/lib/components'
import LeftMenu from '../../components/LeftMenu.vue'
export default {
    components: { LeftMenu },
    setup () {

        const instance = getCurrentInstance();

        const menus = [
            { text: '节点配置', component: shallowRef(Client) },
            { text: '通信加密', component: shallowRef(Encode) },
            { text: 'tcp转发', component: shallowRef(TcpForward) },
            { text: 'udp转发', component: shallowRef(UdpForward) },
            { text: 'http代理', component: shallowRef(HttpProxy) },
            { text: 'socks5代理', component: shallowRef(Socks5) },
            { text: '虚拟网卡组网', component: shallowRef(Vea) },
        ];
        const leftMenus = menus.map(c => {
            return {
                text: c.text
            };
        });
        const state = reactive({
            loading: false,
            currentMenu: 0,
            dom: null,
            menus: menus
        });

        const contentDom = ref(null);
        const onScroll = (e) => {
            let stop = contentDom.value.scrollTop;
            var dividers = contentDom.value.querySelectorAll('.el-divider');
            for (let i = 0; i < dividers.length; i++) {
                if (dividers[i].offsetTop - 10 <= stop) {
                    state.currentMenu = i;
                }
            }
        }
        const listenScroll = () => {
            contentDom.value.addEventListener('scroll', onScroll);
        }
        const removeListenScroll = () => {
            contentDom.value.removeEventListener('scroll', onScroll);
        }
        const handleJumpScroll = (index) => {
            var dividers = contentDom.value.querySelectorAll('.el-divider');
            contentDom.value.scrollTop = dividers[index].offsetTop - 10;
        }

        onMounted(() => {
            nextTick(() => {
                listenScroll();
            });
        });
        onBeforeUnmount(() => {
            removeListenScroll();
        });

        const getFuns = () => {
            const refs = instance.refs;
            const promises = [];
            for (let j in refs) {
                if (j.indexOf('setting_item') == 0) {
                    promises.push(refs[j].submit);
                }
            }
            return promises;
        }
        const handleSave = () => {
            state.loading = true;

            const promises = getFuns();
            const fun = (index = 0) => {
                if (index >= promises.length) {
                    state.loading = false;
                    ElMessage.success('已应用');
                    return;
                }
                promises[index]().then(() => {
                    fun(++index);
                }).catch(() => {
                    state.loading = false;
                    ElMessage.success('有误');
                });
            }
            fun();
        }

        return { state, contentDom, leftMenus, handleJumpScroll, handleSave }
    }
}
</script>

<style lang="stylus" scoped>
.content {
    .inner {
        padding-bottom: 2rem;
        box-sizing: border-box;
    }

    .btn {
        border-top: 1px solid #ddd;
        padding: 1rem;
        text-align: center;
        box-shadow: -1px 1px 0.6rem rgba(0, 0, 0, 0.05);
    }
}

.setting-item {
    margin: 0 2rem 0rem 2rem;
    border: 1px solid #ececec;
    padding: 2rem;
    border-radius: 4px;
    box-shadow: 0 0 0.4rem 0.1rem rgba(0, 0, 0, 0.03) inset;
}
</style>