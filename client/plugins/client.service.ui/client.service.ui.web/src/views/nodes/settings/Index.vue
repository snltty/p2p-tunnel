<template>
    <div class="absolute flex">
        <div class="content h-100 flex-1 flex flex-column">
            <div class="wrap flex-1 scrollbar">
                <el-tabs type="border-card">
                    <el-tab-pane label="主页">
                        <div class="inner " ref="contentDom">
                            <template v-for="(item,index) in leftMenus" :key="index">
                                <div class="setting-item" :class="{animate:state.animate}" :style="`animation-delay:${index*0.1}s`">
                                    <component :is="item.component.value" :ref="`setting_item_${item.text}`" />
                                </div>
                            </template>
                        </div>
                    </el-tab-pane>
                </el-tabs>
            </div>
            <div class="btn" v-if="leftMenus.length > 0">
                <el-button type="primary" size="default" @click="handleSave" :loading="state.loading">应用更改</el-button>
            </div>
        </div>
    </div>
</template> 

<script>
import { getCurrentInstance, computed, watch, reactive, ref, shallowRef, onMounted } from '@vue/runtime-core'
import { useRouter } from 'vue-router'
import { injectServices, accessService } from '../../../states/services'
import { ElMessage } from 'element-plus/lib/components'
import plugin from './plugin'
export default {
    plugin: plugin,
    setup() {

        const instance = getCurrentInstance();

        const files = require.context('../', true, /Setting\.vue/);
        const settings = files.keys().map(c => files(c).default);
        const _menus = settings.sort((a, b) => {
            return (a.plugin.order || 0) - (b.plugin.order || 0);
        }).map(c => {
            return {
                text: c.text || c.plugin.text,
                component: shallowRef(c)
            }
        });

        const router = useRouter();
        const servicesState = injectServices();
        const getMenuIndex = (menus) => {
            for (let i = 0; i < menus.length; i++) {
                if (menus[i].url == router.currentRoute.value.name) {
                    state.currentMenu = i;
                    return;
                }
            }
        }
        const leftMenus = computed(() => {
            let menus = _menus.filter(c => {
                if (c.component.value.serviceCallback) {
                    return c.component.value.serviceCallback(servicesState);
                }
                return accessService(c.component.value.plugin.service, servicesState);
            }).map(c => {
                return {
                    text: c.text, component: c.component
                }
            });
            getMenuIndex(menus);
            return menus;
        });
        watch(() => router.currentRoute.value.name, (name) => {
            getMenuIndex(leftMenus.value);
        }, { immediate: true });


        const state = reactive({
            loading: false,
            currentMenu: 0,
            animate: false
        });
        onMounted(() => {
            setTimeout(() => {
                state.animate = true;
            }, 1000);
        })

        const contentDom = ref(null);
        const getFuns = () => {
            const refs = instance.refs;
            const promises = [];
            for (let j in refs) {
                if (j.indexOf('setting_item') == 0 && refs[j][0]) {
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

        return { state, contentDom, leftMenus, handleSave }
    }
}
</script>

<style lang="stylus" scoped>
.content {
    .wrap {
        padding: 2rem;
        box-sizing: border-box;
    }

    .btn {
        border-top: 1px solid var(--main-border-color);
        padding: 1rem;
        text-align: center;
        box-shadow: -1px 1px 0.6rem rgba(0, 0, 0, 0.05);
        background-color: #fff;
    }
}

.setting-item {
    transition: 0.3s;
    opacity: 0;
    transform: translate3d(0, -20px, 0);
    opacity: 1;
    transform: translate3d(0, 0, 0);
    animation: bounceInDown 0.3s;
    animation-fill-mode: both;

    .el-divider {
        margin-top: 0;
    }
}
</style>