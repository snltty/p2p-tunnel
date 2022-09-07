<!--
 * @Author: snltty
 * @Date: 2021-09-26 21:05:30
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-26 22:21:39
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\ContextMenu.vue
-->
<template>
    <div class="context-menu" v-if="isShow" :style="{left:`${x}px`,top:`${y}px`}">
        <ul>
            <template v-for="(item ,index) in menus" :key="index">
                <li @click="item.handle()">{{item.text}}</li>
            </template>
        </ul>
    </div>
</template>
<script>
import { onMounted, onUnmounted, reactive, toRefs } from '@vue/runtime-core';
export default {
    setup () {
        const state = reactive({
            isShow: false,
            menus: [],
            x: 0,
            y: 0
        })
        const show = (event, menus) => {
            state.x = event.pageX;
            state.y = event.pageY;
            state.menus = menus;
            state.isShow = true;
        }
        const documentClick = () => {
            state.isShow = false;
        }
        onMounted(() => {
            document.addEventListener('click', documentClick);
        });
        onUnmounted(() => {
            document.removeEventListener('click', documentClick);
        });

        return {
            ...toRefs(state), show
        }
    }
}
</script>

<style lang="stylus" scoped>
.context-menu
    position: fixed;
    background-color: #FFF;
    z-index: 999;
    border: 1px solid #dadada;
    border-radius: 0.2rem;
    box-shadow: 0.1rem 0.1rem 0.6rem rgba(0, 0, 0, 0.1);

    li
        padding: 0.6rem 1rem;
        cursor: pointer;

        &:hover
            background-color: #f5f5f5;
</style>