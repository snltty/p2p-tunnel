<!--
 * @Author: snltty
 * @Date: 2023-02-12 22:58:50
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 23:17:53
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\components\LeftMenu.vue
-->
<template>
    <div class="menu h-100">
        <ul>
            <template v-for="(item,index) in menus" :key="index">
                <li>
                    <template v-if="item.url">
                        <router-link :to="{name:item.url}" :class="{current:currentMenu == index}">{{item.text}}</router-link>
                    </template>
                    <template v-else>
                        <a href="javascript:;" :class="{current:currentMenu == index}" @click="handleJumpScroll(index)">{{item.text}}</a>
                    </template>
                </li>
            </template>
        </ul>
    </div>
</template>

<script>
import { computed } from '@vue/reactivity'
export default {
    props: ['menus', 'modelValue'],
    emits: ['update:modelValue', 'handle'],
    setup (props, { emit }) {
        const current = computed(() => props.modelValue);
        const handleJumpScroll = (index) => {
            emit('handle', index);
        }
        return {
            menus: props.menus,
            currentMenu: current,
            handleJumpScroll
        }
    }
}
</script>

<style lang="stylus" scoped>
.menu {
    width: 14rem;
    border-right: 1px solid #ddd;
    box-shadow: 1px 1px 0.6rem rgba(0, 0, 0, 0.05);
    background-color: #fff;
    transition: 0.3s cubic-bezier(0.56, -0.37, 0.78, 1.66);

    ul {
        padding: 1rem 1rem;

        li {
            margin-bottom: 1rem;

            a {
                padding: 0.6rem 1rem;
                color: #555;
                display: block;
                font-size: 1.4rem;
                transition: 0.3s;
                border-width: 1px;
                border-style: solid;
                border-color: transparent;
                background-color: transparent;
                border-radius: 1rem;

                &:hover, &.current {
                    background-color: #fff;
                    border-color: #b1d7c1;
                    background-color: #2c73490d;
                    color: #3b9c64;
                }
            }
        }
    }
}
</style>