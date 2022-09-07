<!--
 * @Author: snltty
 * @Date: 2021-10-02 14:16:32
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-18 13:26:45
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\Logger.vue
-->
<template>
    <div class="logger-setting-wrap flex flex-column h-100">
        <div class="head flex">
            <el-select v-model="Type" size="small" @change="loadData" style="width:6rem;">
                <el-option :value="-1" label="全部"></el-option>
                <el-option :value="0" label="debug"></el-option>
                <el-option :value="1" label="info"></el-option>
                <el-option :value="2" label="debug"></el-option>
                <el-option :value="3" label="error"></el-option>
            </el-select>
            <span class="split"></span>
            <el-button size="small" :loading="loading" @click="loadData">刷新列表</el-button>
            <el-button type="warning" size="small" :loading="loading" @click="clearData">清空</el-button>
            <span class="flex-1"></span>
            <ConfigureModal className="LoggerClientConfigure">
                <el-button size="small">配置插件</el-button>
            </ConfigureModal>
        </div>
        <div class="body flex-1 relative">
            <div class="absolute">
                <el-table border :data="page.Data" size="small" height="100%" :row-class-name="tableRowClassName">
                    <el-table-column type="index" width="50" />
                    <el-table-column prop="Type" label="类别" width="80">
                        <template #default="scope">
                            <span>{{types[scope.row.Type]}} </span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="Time" label="时间" width="160"></el-table-column>
                    <el-table-column prop="Content" label="内容"></el-table-column>
                </el-table>
            </div>
        </div>
        <div class="pages t-c">
            <el-pagination :total="page.Count" v-model:currentPage="page.PageIndex" :page-size="page.PageSize" @current-change="loadData" background layout="total,prev, pager, next">
            </el-pagination>
        </div>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity'
import { getLoggers, clearLoggers } from '../../apis/logger'
import ConfigureModal from './configure/ConfigureModal.vue'
import { onMounted } from '@vue/runtime-core'
export default {
    components: { ConfigureModal },
    setup () {

        const state = reactive({
            loading: true,
            page: { PageIndex: 1, PageSize: 20 },
            types: ['debug', 'info', 'warning', 'error'],
            Type: -1
        })
        const loadData = () => {
            state.loading = true;
            let json = JSON.parse(JSON.stringify(state.page));
            json['Type'] = state.Type;
            getLoggers(json).then((res) => {
                state.loading = false;
                res.Data.map(c => {
                    c.Time = new Date(c.Time).format('yyyy-MM-dd hh:mm:ss')
                });
                state.page = res;
            }).catch(() => {
                state.loading = false;
            });
        }
        const clearData = () => {
            state.loading = true;
            clearLoggers().then(() => {
                state.loading = false;
                loadData();
            }).catch(() => {
                state.loading = false;
            });
        }
        onMounted(() => {
            loadData();
        });

        const tableRowClassName = ({ row, rowIndex }) => {
            return `type-${row.Type}`;
        }

        return {
            ...toRefs(state), loadData, clearData, tableRowClassName
        }
    }
}
</script>
<style lang="stylus" scoped>
.pages
    padding: 1rem;

.logger-setting-wrap
    padding: 2rem;
    box-sizing: border-box;

    .head
        margin-bottom: 1rem;
</style>
<style  lang="stylus">
.logger-setting-wrap
    .el-table
        .type-0
            color: blue;

        .type-1
            color: #333;

        .type-2
            color: #cd9906;

        .type-3
            color: red;
</style>