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
import { reactive } from '@vue/reactivity';
import { useRouter } from 'vue-router'
import { watch } from '@vue/runtime-core';
export default {
    components: { LeftMenu },
    setup() {

        const state = reactive({
            currentMenu: 0
        });
        const router = useRouter();
        const leftMenus = router.options.routes.filter(c => c.name == 'Nodes')[0].children.map(c => {
            return {
                text: c.meta.name,
                url: c.name
            }
        });
        watch(() => router.currentRoute.value.name, (name) => {
            for (let i = 0; i < leftMenus.length; i++) {
                if (leftMenus[i].url == name) {
                    state.currentMenu = i;
                    break;
                }
            }
        }, { immediate: true })

        return {
            leftMenus, state
        }
    }
}
</script>

<style lang="stylus" scoped></style>