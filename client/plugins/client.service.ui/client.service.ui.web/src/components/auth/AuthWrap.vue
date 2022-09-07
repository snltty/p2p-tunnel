<!--
 * @Author: xr
 * @Date: 2021-03-23 19:30:59
 * @LastEditors: xr
 * @LastEditTime: 2022-01-23 14:13:51
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\components\auth\AuthWrap.vue
-->
<template>
    <slot></slot>
</template>
<script>
import { ref } from '@vue/reactivity';
import { getServices } from '../../apis/configure'
import { provide } from '@vue/runtime-core';
export default {
    setup () {
        const services = ref([]);
        getServices().then((res) => {
            services.value = res;
        });
        provide('btn-auth-services', services);
    }
}
</script>