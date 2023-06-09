<template>
    <div class="absolute flex">
        <div class="menu h-100">
            <LeftMenu :menus="leftMenus" v-model="state.currentMenu"></LeftMenu>
        </div>
        <div class="content relative h-100 flex-1 scrollbar">
            <router-view v-if="accessService($route.meta.service,servicesState)" v-slot="{ Component }">
                <transition name="route-animate">
                    <component :is="Component" />
                </transition>
            </router-view>
            <NotAccess v-else></NotAccess>
        </div>
    </div>
</template>
  
<script>
import LeftMenu from '../../components/LeftMenu.vue'
import NotAccess from '../../components/NotAccess.vue'
import { computed, reactive } from '@vue/reactivity';
import { useRouter } from 'vue-router'
import { watch } from '@vue/runtime-core';
import { accessService, injectServices } from '../../states/services'
import { shareData } from '../../states/shareData'
import { injectSignIn } from '../../states/signin'
export default {
    components: { LeftMenu, NotAccess },
    setup() {

        const state = reactive({
            currentMenu: 0
        });
        const router = useRouter();
        const servicesState = injectServices();
        const signinState = injectSignIn();
        const serviceAccess = computed(() => signinState.RemoteInfo.Access);
        const hasAccess = (access) => {
            return shareData.serverAccessHas(serviceAccess.value, access);
        }

        const getMenuIndex = (menus) => {
            for (let i = 0; i < menus.length; i++) {
                if (menus[i].url == router.currentRoute.value.name) {
                    state.currentMenu = i;
                    return;
                }
            }
        }
        const leftMenus = computed(() => {
            let menus = router.options.routes
                .filter(c => c.name == 'Nodes')[0].children
                .filter(c => accessService(c.meta.service, servicesState)).map(c => {
                    return {
                        text: c.meta.text,
                        url: c.name
                    }
                });
            getMenuIndex(menus);
            return menus;
        });
        watch(() => router.currentRoute.value.name, (name) => {
            getMenuIndex(leftMenus.value);
        }, { immediate: true });

        return {
            leftMenus, state, servicesState, accessService
        }
    }
}
</script>

<style lang="stylus" scoped></style>