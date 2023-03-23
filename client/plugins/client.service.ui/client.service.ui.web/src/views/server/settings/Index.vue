<template>
    <div class="absolute flex">
        <div class="content h-100 flex-1 flex flex-column">
            <div class="inner flex-1 scrollbar" ref="contentDom">
                <template v-for="(item,index) in leftMenus" :key="index">
                    <el-divider content-position="left" border-style="dotted">{{item.text}}</el-divider>
                    <div class="setting-item">
                        <component :is="item.component.value" :ref="`setting_item_${item.text}`" />
                    </div>
                </template>
            </div>
            <div class="btn" v-if="leftMenus.length > 0">
                <el-button type="primary" size="default" @click="handleSave" :loading="state.loading">应用更改</el-button>
            </div>
        </div>
    </div>
</template> 

<script>
import Setting from '../Setting.vue'
// import TcpForward from '../../nodes/tcpforward/ServerSetting.vue'
// import UdpForward from '../../nodes/udpforward/ServerSetting.vue'
// import Socks5 from '../../nodes/socks5/ServerSetting.vue'
import { getCurrentInstance, nextTick, onBeforeUnmount, computed, onMounted, reactive, ref, shallowRef } from '@vue/runtime-core'
import { injectServices, accessService } from '../../../states/services'
import { injectSignIn } from '../../../states/signin'
import { ElMessage } from 'element-plus/lib/components'
export default {
    setup() {

        const instance = getCurrentInstance();
        const signinState = injectSignIn();
        const serviceAccess = computed(() => signinState.RemoteInfo.Access);
        const _menus = [
            { text: '服务器配置', component: shallowRef(Setting) },
            // { text: 'tcp转发', component: shallowRef(TcpForward) },
            // { text: 'udp转发', component: shallowRef(UdpForward) },
            // { text: 'socks5代理', component: shallowRef(Socks5) }
        ];
        const servicesState = injectServices();
        const leftMenus = computed(() => {
            let menus = _menus.filter(c => accessService(c.component.value.service, servicesState) && (c.component.value.access & serviceAccess.value) == c.component.value.access)
                .map(c => {
                    return {
                        text: c.text, component: c.component
                    }
                });
            return menus;
        });


        const state = reactive({
            loading: false,
            currentMenu: 0
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
                    promises.push(refs[j][0].submit);
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
        border-top: 1px solid var(--main-border-color);
        padding: 1rem;
        text-align: center;
        box-shadow: -1px 1px 0.6rem rgba(0, 0, 0, 0.05);
    }
}

.setting-item {
    margin: 0 2rem 0rem 2rem;
    border: 1px solid #ececec;
    background-color: #fff;
    padding: 2rem;
    border-radius: 4px;
}
</style>