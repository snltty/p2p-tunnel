<template>
    <div class="logger-setting-wrap flex flex-column h-100">
        <div class="inner">
            <div class="head flex">
                <el-select v-model="state.Type" size="small" @change="loadData" style="width:6rem;">
                    <el-option :value="-1" label="全部"></el-option>
                    <el-option :value="0" label="debug"></el-option>
                    <el-option :value="1" label="info"></el-option>
                    <el-option :value="2" label="warning"></el-option>
                    <el-option :value="3" label="error"></el-option>
                </el-select>
                <span class="split"></span>
                <el-button size="small" :loading="state.loading" @click="loadData">刷新列表</el-button>
                <el-button type="warning" size="small" :loading="state.loading" @click="clearData">清空</el-button>
                <span class="flex-1"></span>
            </div>
            <div class="body flex-1 relative">
                <div v-if="state.page.Data.length > 0">
                    <el-table border :data="state.page.Data" size="small" height="100%" @row-click="handleRowClick" :row-class-name="tableRowClassName">
                        <el-table-column type="index" width="50" />
                        <el-table-column prop="Type" label="类别" width="80">
                            <template #default="scope">
                                <span>{{state.types[scope.row.Type]}} </span>
                            </template>
                        </el-table-column>
                        <el-table-column prop="Time" label="时间" width="160"></el-table-column>
                        <el-table-column prop="content" label="内容"></el-table-column>
                    </el-table>
                </div>
                <el-empty v-else />
            </div>
            <div class="pages t-c">
                <el-pagination small :total="state.page.Count" v-model:currentPage="state.page.Page" :page-size="state.page.PageSize" @current-change="loadData" background layout="total,prev, pager, next">
                </el-pagination>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import { getLoggers, clearLoggers } from '../../../apis/logger'
import { onMounted } from '@vue/runtime-core'
import { ElMessageBox } from 'element-plus/lib/components'
import plugin from './plugin'
export default {
    plugin: plugin,
    components: {},
    setup() {

        const state = reactive({
            loading: true,
            page: { Page: 1, PageSize: 10, Count: 0, Data: [] },
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
                    c.Time = new Date(c.Time).format('yyyy-MM-dd hh:mm:ss');
                    c.content = c.Content.substring(0, 50);
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
        const handleRowClick = (row, column, event) => {
            let css = `padding:1rem;border:1px solid #ddd; resize:none;width:100%;box-sizing: border-box;white-space: nowrap; height:30rem;`;
            ElMessageBox.alert(`<textarea class="scrollbar-4" style="${css}">${row.Content}</textarea>`, '内容', {
                dangerouslyUseHTMLString: true,
            });
        }

        return {
            state, loadData, clearData, tableRowClassName, handleRowClick
        }
    }
}
</script>
<style lang="stylus" scoped>
.pages {
    padding: 1rem 0 0 1rem;
}

.logger-setting-wrap {
    padding: 2rem;
    box-sizing: border-box;

    .inner {
        padding: 1rem;
        background-color: #fff;
        border-radius: 4px;
        border: 1px solid #ddd;
        box-shadow: 0 0 8px 1px rgba(0, 0, 0, 0.05);
    }

    .head {
        margin-bottom: 1rem;
    }
}
</style>
<style  lang="stylus">
.logger-setting-wrap {
    .el-table {
        .type-0 {
            color: blue;
        }

        .type-1 {
            color: #333;
        }

        .type-2 {
            color: #cd9906;
        }

        .type-3 {
            color: red;
        }
    }
}
</style>