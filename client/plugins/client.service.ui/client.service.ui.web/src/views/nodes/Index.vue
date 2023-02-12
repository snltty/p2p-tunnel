<template>
    <div class="absolute flex">
        <div class="menu h-100">
            <LeftMenu :menus="leftMenus" v-model="state.currentMenu"></LeftMenu>
        </div>
        <div class="content h-100 flex-1 scrollbar">
            <Clients></Clients>
        </div>
    </div>
</template>

<script>
import Clients from './Clients.vue'
import LeftMenu from '../../components/LeftMenu.vue'
import { reactive } from '@vue/reactivity';
import { useRoute } from 'vue-router'
export default {
    components: { LeftMenu, Clients },
    setup () {

        const route = useRoute();
        const leftMenus = [
            { text: '节点', url: 'Nodes' }
        ];
        const state = reactive({
            currentMenu: 0
        });

        return {
            leftMenus, state
        }
    }
}
</script>

<style lang="stylus" scoped></style>