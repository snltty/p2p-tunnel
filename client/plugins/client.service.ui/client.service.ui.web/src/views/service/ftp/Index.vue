<!--
 * @Author: snltty
 * @Date: 2021-09-26 19:10:48
 * @LastEditors: snltty
 * @LastEditTime: 2021-10-18 21:26:05
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.web.vue3\src\views\plugin\ftp\Index.vue
-->
<template>
    <div class="ftp-wrap h-100 flex flex-column flex-nowrap">
        <div class="flex-1">
            <List></List>
        </div>
        <div class="split"></div>
        <div>
            <Progress></Progress>
        </div>
    </div>
</template>

<script>
import Progress from './Progress.vue'
import List from './List.vue'
import { provideFilesData } from './list-share-data'
export default {
    components: { List, Progress },
    setup () {
        provideFilesData();
        return {}
    }
}
</script>

<style lang="stylus" scoped>
.el-table::before
    height: 0;

.ftp-wrap
    padding: 1rem;
    box-sizing: border-box;

    .split
        height: 0.6rem;
</style>
<style lang="stylus">
.el-input.file-name
    .el-input__inner
        border: 0;
</style>