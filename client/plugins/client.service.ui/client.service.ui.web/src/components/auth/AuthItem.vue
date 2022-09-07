<!--
 * @Author: xr
 * @Date: 2021-03-23 20:41:22
 * @LastEditors: xr
 * @LastEditTime: 2022-01-23 14:13:31
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\components\auth\AuthItem.vue
-->
<template>
    <template v-if="services.indexOf(name) >= 0">
        <slot></slot>
    </template>
</template>

<script>
import { inject } from 'vue'
export default {
    props: ['name'],
    setup () {
        const services = inject('btn-auth-services');
        return {
            services
        }
    }
}
</script>
<style lang="stylus" scoped></style>