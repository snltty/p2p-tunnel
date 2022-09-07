<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:49:58
 * @LastEditors: snltty
 * @LastEditTime: 2021-09-30 18:47:50
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\List.vue
-->
<template>
    <div class="list flex flex-nowrap h-100">
        <div class="local flex-1">
            <Local></Local>
        </div>
        <span class="split"></span>
        <div class="remote flex-1">
            <Remote></Remote>
        </div>
    </div>
</template>

<script>
import Local from './Local.vue'
import Remote from './Remote.vue'
export default {
    components: { Local, Remote },
    setup () {

        return {}
    }
}
</script>

<style lang="stylus" scoped></style>