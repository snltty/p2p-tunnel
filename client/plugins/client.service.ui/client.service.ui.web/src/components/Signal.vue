<!--
 * @Author: snltty
 * @Date: 2022-10-23 01:39:31
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 01:50:17
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\components\Signal.vue
-->
<template>
    <div :class="`flex signal-${classValue}`">
        <div class="signal flex">
            <div class="item-1"></div>
            <div class="item-2"></div>
            <div class="item-3"></div>
            <div class="item-4"></div>
            <div class="item-5"></div>
        </div>
        <span>{{value}}ms</span>
    </div>
</template>

<script>
import { computed } from '@vue/reactivity'
export default {
    props: ['value'],
    setup (props) {

        const step = [1000, 500, 100, 50, 30];
        const classValue = computed(() => {
            if (props.value == -1) return props.value;
            for (let i = 1; i <= step.length; i++) {
                if (props.value >= step[i - 1]) {
                    return i;
                }
            }
            return step.length;
        });
        return {
            classValue
        }
    }
}
</script>

<style lang="stylus" scoped>
.signal
    align-content: space-around;
    align-items: flex-end;

    div
        width: 4px;
        background-color: #ddd;
        margin-right: 1px;

    .item-1
        height: 2px;

    .item-2
        height: 4px;

    .item-3
        height: 6px;

    .item-4
        height: 8px;

    .item-5
        height: 10px;

&.signal-1
    color: red;

    .item-1
        background-color: red;

&.signal-2
    color: #ffab00;

    .item-1, .item-2
        background-color: #ffab00;

&.signal-3
    color: #d5d30b;

    .item-1, .item-2, .item-3
        background-color: #d5d30b;

&.signal-4
    color: #6be334;

    .item-1, .item-2, .item-3, .item-4
        background-color: #6be334;

&.signal-5
    color: rgb(20, 135, 39);

    .item-1, .item-2, .item-3, .item-4, .item-5
        background-color: rgb(20, 135, 39);
</style>