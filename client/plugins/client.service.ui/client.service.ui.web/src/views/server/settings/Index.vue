<template>
    <div class="absolute flex">
        <div class="content h-100 flex-1 flex flex-column">
            <div class="inner flex-1 scrollbar">
                <el-tabs type="border-card">
                    <el-tab-pane label="主页">
                        <template v-for="(item,index) in leftMenus" :key="index">
                            <div class="setting-item" :style="`animation-delay:${index*0.1}s`">
                                <component :is="item.component.value" :ref="`setting_item_${item.text}`" />
                            </div>
                        </template>
                    </el-tab-pane></el-tabs>
            </div>
            <div class="btn" v-if="leftMenus.length > 0">
                <el-button type="primary" size="default" @click="handleSave" :loading="state.loading">应用更改</el-button>
            </div>
        </div>
    </div>
</template> 

<script>
import { getCurrentInstance, computed, reactive, shallowRef } from '@vue/runtime-core'
import { injectServices, accessService } from '../../../states/services'
import { shareData } from '../../../states/shareData'
import { injectSignIn } from '../../../states/signin'
import { ElMessage } from 'element-plus/lib/components'
import plugin from './plugin'
export default {
    plugin: plugin,
    setup() {

        const instance = getCurrentInstance();
        const signinState = injectSignIn();
        const serviceAccess = computed(() => signinState.RemoteInfo.Access);

        const files = require.context('../', true, /Setting\.vue/);
        const settings = files.keys().map(c => files(c).default);
        const _menus = settings.sort((a, b) => {
            return (a.plugin.order || 0) - (b.plugin.order || 0);
        }).map(c => {
            return {
                text: c.plugin.text,
                component: shallowRef(c)
            }
        });
        const servicesState = injectServices();
        const leftMenus = computed(() => {
            let menus = _menus.filter(c => accessService(c.component.value.plugin.service, servicesState) && shareData.serverAccessHas(serviceAccess.value, c.component.value.plugin.access))
                .map(c => {
                    return {
                        text: c.text, component: c.component
                    }
                });
            return menus;
        });


        const state = reactive({
            loading: false,
        });


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

        return { state, leftMenus, handleSave }
    }
}
</script>

<style lang="stylus" scoped>
.content {
    .inner {
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
    margin: 2rem;
    background-color: #fff;
    animation: bounceInDown 0.3s;
    animation-fill-mode: both;

    .el-divider {
        margin-top: 0;
    }
}
</style>