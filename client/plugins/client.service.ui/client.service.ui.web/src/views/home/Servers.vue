<!--
 * @Author: snltty
 * @Date: 2023-02-10 17:33:52
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-10 22:00:16
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Servers.vue
-->
<template>
    <div class="servers-mark absolute" :class="{show:animation}" @click="handleClose">
        <div class="servers-wrap absolute scrollbar" @click.stop>
            <ul>
                <li class="flex">
                    <div class="country-img">
                        <img src="../../assets/zg.png">
                    </div>
                    <div class="country-name">中国</div>
                    <div class="flex-1"></div>
                    <div class="country-time">
                        <Signal :value="30"></Signal>
                    </div>
                </li>
                <li class="flex">
                    <div class="country-img">
                        <img src="../../assets/hg.png">
                    </div>
                    <div class="country-name">韩国</div>
                    <div class="flex-1"></div>
                    <div class="country-time">
                        <Signal :value="30"></Signal>
                    </div>
                </li>
                <li class="flex">
                    <div class="country-img">
                        <img src="../../assets/rb.png">
                    </div>
                    <div class="country-name">日本</div>
                    <div class="flex-1"></div>
                    <div class="country-time">
                        <Signal :value="30"></Signal>
                    </div>
                </li>
                <li class="flex">
                    <div class="country-img">
                        <img src="../../assets/mg.png">
                    </div>
                    <div class="country-name">美国</div>
                    <div class="flex-1"></div>
                    <div class="country-time">
                        <Signal :value="30"></Signal>
                    </div>
                </li>
                <li class="flex">
                    <div class="country-img">
                        <img src="../../assets/zgxg.png">
                    </div>
                    <div class="country-name">中国香港</div>
                    <div class="flex-1"></div>
                    <div class="country-time">
                        <Signal :value="30"></Signal>
                    </div>
                </li>
                <li class="flex">
                    <div class="country-img">
                        <img src="../../assets/xjp.png">
                    </div>
                    <div class="country-name">新加坡</div>
                    <div class="flex-1"></div>
                    <div class="country-time">
                        <Signal :value="30"></Signal>
                    </div>
                </li>
            </ul>
        </div> 
    </div>
</template>  
   
<script>
import { onMounted, ref, watch } from '@vue/runtime-core'
import Signal from './Signal.vue'
export default {
    components:{Signal},
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup (props, { emit }) {

        const animation = ref(false);
        watch(() => animation.value, (val) => {
            if (val == false) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        onMounted(()=>{
            setTimeout(()=>{
                animation.value = true;
            });
        });
        const handleClose = ()=>{
            animation.value = false;
        }
 
        return {animation,handleClose}
    }
}
</script>

<style lang="stylus" scoped>
.servers-mark
    background-color: rgba(0,0,0,0.05);
    &.show
        .servers-wrap
            left: 100%;
            transform: translateX(-20rem);
.servers-wrap
    left: 200%;
    transform: translateX(0);
    right: auto;
    width: 20rem;
    border-left: 1px solid #ddd;
    box-shadow: -1px -1px .6rem rgba(0,0,0,0.05);
    background-color: #fff;
    transition: .3s;


    ul
        padding: 1rem
li
    cursor: pointer;
    margin: 0 auto;
    border: 1px solid #ddd;
    width: 100%;
    padding: 0.6rem;
    border-radius: 0.4rem;
    transition: 0.3s;
    box-sizing: border-box;
    margin-bottom: 1rem

    &:hover
        box-shadow: 0 0 0 0.4rem #d1d8e261;

    .country-img
        font-size: 0;
        margin-right: 0.6rem;

        img
            height: 2rem;

    .country-name
        line-height: 2rem;
        color: #555

    .country-time
        padding-top: 0.2rem;
</style>