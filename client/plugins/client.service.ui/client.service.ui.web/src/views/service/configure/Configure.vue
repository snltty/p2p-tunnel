<!--
 * @Author: snltty
 * @Date: 2021-10-08 09:11:34
 * @LastEditors: snltty
 * @LastEditTime: 2022-04-30 16:06:11
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\configure\Configure.vue
-->
<template>
    <div class="plugin-setting-wrap">
        <div class="head">
            <el-button size="small" @click="getData">刷新列表</el-button>
        </div>
        <el-table v-loading="loading" :data="list" border size="small">
            <el-table-column prop="Name" label="插件名"></el-table-column>
            <el-table-column prop="Desc" label="描述"></el-table-column>
            <el-table-column prop="Author" label="作者"></el-table-column>
            <el-table-column prop="enable" label="启用" width="80" class="t-c">
                <template #default="scope">
                    <el-switch v-model="scope.row.Enable" @change="handleEnableChange(scope.row)"></el-switch>
                </template>
            </el-table-column>
            <el-table-column prop="todo" label="操作" width="80" class="t-c">
                <template #default="scope">
                    <ConfigureModal :className="scope.row.ClassName" @success="getData" :key="scope.row.ClassName">
                        <el-button size="small">配置</el-button>
                    </ConfigureModal>
                </template>
            </el-table-column>
        </el-table>
    </div>
</template>
<script>
import { reactive, ref, toRefs } from '@vue/reactivity'
import { getConfigures, enableConfigure } from '../../../apis/configure'
import ConfigureModal from './ConfigureModal.vue'
export default {
    components: { ConfigureModal },
    setup () {
        const editor = ref(null);
        const state = reactive({
            loading: false,
            showAdd: false,
            list: [],
            rules: {
            }
        });
        const getData = () => {
            getConfigures().then((res) => {
                state.list = res;
            });
        };
        getData();

        const handleEnableChange = (row) => {
            enableConfigure(row.ClassName, row.Enable).then(() => {
                getData();
            }).catch(() => {
                row.Enable = !row.Enable;
            });
        }

        return {
            ...toRefs(state), editor, getData, handleEnableChange
        }
    }
}
</script>
<style lang="stylus" scoped>
.plugin-setting-wrap
    padding: 2rem;

.head
    margin-bottom: 0.6rem;
</style>