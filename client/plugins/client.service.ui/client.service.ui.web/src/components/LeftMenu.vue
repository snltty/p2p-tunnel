<template>
    <div class="menu h-100" :class="className">
        <div class="btn" @click="handleShow">
            <el-icon v-if="state">
                <DArrowLeft />
            </el-icon>
            <el-icon v-else>
                <DArrowRight />
            </el-icon>
        </div>
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
import { computed, ref } from '@vue/reactivity'
export default {
    props: ['menus', 'modelValue'],
    emits: ['update:modelValue', 'handle'],
    setup(props, { emit }) {

        const state = ref(window.innerWidth > 798);
        const className = computed(c => state.value.toString());
        const current = computed(() => props.modelValue);
        const menus = computed(() => props.menus);
        const handleJumpScroll = (index) => {
            emit('handle', index);
        }
        const handleShow = () => {
            state.value = !state.value;
        }

        return {
            state,
            className,
            menus,
            currentMenu: current,
            handleJumpScroll,
            handleShow
        }
    }
}
</script>

<style lang="stylus" scoped>
.menu {
    border-right: 1px solid var(--main-border-color);
    box-shadow: 1px 1px 0.6rem 0.1rem rgba(0, 0, 0, 0.05);
    background-color: #fff;
    transition: 0.3s cubic-bezier(0.56, -0.37, 0.78, 1.66);
    position: relative;
    z-index: 9;

    &.true {
        width: 14rem;
    }

    &.false {
        width: 0;
    }

    .btn {
        position: absolute;
        right: -2.2rem;
        top: 40%;
        width: 2rem;
        padding: 2rem 0;
        text-align: center;
        background-color: #fff;
        border-width: 1px 1px 1px 0;
        border-style: solid;
        border-color: var(--main-border-color);
        border-radius: 0 4px 4px 0px;
        cursor: pointer;
        color: #555;

        &:hover {
            box-box-shadow: 0 0 4px rgba(0, 0, 0, 1);
        }
    }

    &.false ul {
        display: none;
    }

    ul {
        padding: 1rem 1rem;
        overflow: hidden;

        li {
            margin-bottom: 1rem;
            white-space: nowrap;

            a {
                padding: 0.6rem 1rem;
                color: var(--left-menu-color);
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