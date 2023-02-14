<template>
    <div class="absolute flex">
        <div class="menu h-100">
            <LeftMenu :menus="leftMenus" v-model="state.currentMenu"></LeftMenu>
        </div>
        <div class="content h-100 flex-1 scrollbar">
            <router-view></router-view>
        </div>
    </div>
</template>
  
<script>
import LeftMenu from '../../components/LeftMenu.vue'
import { computed, reactive } from '@vue/reactivity';
import { useRouter } from 'vue-router'
import { watch } from '@vue/runtime-core';
import { injectServices } from '../../states/services'
export default {
    components: { LeftMenu },
    setup() {

        const state = reactive({
            currentMenu: 0
        });
        const router = useRouter();
        const servicesState = injectServices();

        const leftMenus = computed(() => {
            return router.options.routes
                .filter(c => c.name == 'Nodes')[0].children
                .filter(c => servicesState.services.indexOf(c.meta.service) >= 0 || !c.meta.service).map(c => {
                    return {
                        text: c.meta.name,
                        url: c.name
                    }
                });
        });
        watch(() => router.currentRoute.value.name, (name) => {
            for (let i = 0; i < leftMenus.value.length; i++) {
                if (leftMenus.value[i].url == name) {
                    state.currentMenu = i;
                    return;
                }
            }
            // if (leftMenus.value.length > 0) {
            //     router.push({ name: leftMenus.value[0].url })
            // }
        }, { immediate: true })

        return {
            leftMenus, state
        }
    }
}
</script>

<style lang="stylus" scoped></style>