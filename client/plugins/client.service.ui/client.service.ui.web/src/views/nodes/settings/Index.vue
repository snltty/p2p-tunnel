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
import { getCurrentInstance, computed, watch, reactive, ref, shallowRef } from '@vue/runtime-core'
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
            currentMenu: 0
        });

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