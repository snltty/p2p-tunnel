<!--
 * @Author: snltty
 * @Date: 2022-05-14 19:17:29
 * @LastEditors: snltty
 * @LastEditTime: 2022-10-23 01:10:49
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\wakeup\Index.vue
-->
<template>
    <div class="wakeup-wrap">
        <div class="inner">
            <h3 class="title t-c">
                <span>{{$route.meta.name}}</span>
                <ConfigureModal className="WakeUpClientConfigure">
                    <el-button size="small">配置插件</el-button>
                </ConfigureModal>
                <el-button type="primary" size="small" :loading="loading" @click="handleUpdate">刷新列表</el-button>
            </h3>

            <div>
                <el-table size="small" border :data="state.data" style="width: 100%">
                    <el-table-column prop="name" label="客户端">
                        <template #default="scope">
                            <span>{{scope.row.name}}</span>
                        </template>
                    </el-table-column>
                    <el-table-column prop="macs" label="列表">
                        <template #default="scope">
                            <div class="t-c">
                                <template v-for="(item,index) in scope.row.items" :key="index">
                                    <el-button size="small" @click="handleWakeUp(scope.row.name,item)">{{item.Name}}</el-button>
                                </template>
                            </div>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import { onMounted, onUnmounted } from '@vue/runtime-core'
import { websocketState } from '../../../apis/request'
import { getConfig, getList, wakeup, update } from '../../../apis/wakeup'
import ConfigureModal from '../configure/ConfigureModal.vue'
import { ElMessage } from 'element-plus'

export default {
    components: { ConfigureModal },
    setup () {

        const state = reactive({
            loading: false,
            data: []
        });
        let timer = 0;
        const loadData = () => {
            if (websocketState.connected) {
                Promise.all([getList(), getConfig()]).then(([updates, config]) => {

                    let arr = [];
                    for (let j in updates) {
                        arr.push({
                            name: j,
                            items: updates[j]
                        });
                    }
                    arr.push({ name: '--本机--', items: config.Items });
                    state.data = arr;
                    timer = setTimeout(loadData, 1000);
                });
            } else {
                timer = setTimeout(loadData, 1000);
            }
        }

        onMounted(() => {
            handleUpdate();
            loadData();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        });

        const handleWakeUp = (name, item) => {
            wakeup({
                name: name,
                mac: item.Mac
            }).then((res) => {
                ElMessage.success('已发送');
            });
        }
        const handleUpdate = () => {
            update().then(() => {
                ElMessage.success('已刷新');
            });
        }

        return {
            state, handleWakeUp, handleUpdate
        }
    }
}
</script>

<style lang="stylus" scoped>
.wakeup-wrap
    padding: 2rem;

.inner
    border: 1px solid #ddd;
    padding: 1rem;
    border-radius: 0.4rem;
    margin-bottom: 1rem;

.alert
    margin-bottom: 1rem;

@media screen and (max-width: 768px)
    .el-col
        margin-top: 0.6rem;
</style>