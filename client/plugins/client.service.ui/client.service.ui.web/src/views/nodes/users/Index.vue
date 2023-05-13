<template>
    <div class="forward-wrap">
        <div class="inner">
            <div class="head flex">
                <el-input v-model="state.account" size="small" style="width:10rem;margin:0 .4rem 0 0rem"></el-input>
                <el-button size="small" @click="getData" :loading="state.loading">刷新列表</el-button>
                <span class="flex-1"></span>
                <span>对服务器账号配置在本节点的权限</span>
            </div>
            <div class="content">
                <el-table :data="state.data.Data" stripe border size="small" @sort-change="handleSort">
                    <el-table-column prop="ID" sortable label="账号">
                        <template #default="scope">
                            【{{scope.row.ID}}】{{scope.row.Account}}
                        </template>
                    </el-table-column>
                    <el-table-column prop="EndTime" sortable label="时间" width="140">
                        <template #default="scope">
                            <p>{{scope.row.AddTime}}</p>
                            <p>{{scope.row.EndTime}}</p>
                        </template>
                    </el-table-column>
                    <el-table-column prop="NetFlow" sortable label="流量">
                        <template #default="scope">
                            <span>--</span>
                            <!-- <p>已用 : {{scope.row.NetFlow == -1 ?'//无限' :scope.row.NetFlow.sizeFormat().join('')}}</p>
                            <p>总量 : {{scope.row.NetFlow == -1 ?'//无限' :scope.row.NetFlow.sizeFormat().join('')}}</p> -->
                        </template>
                    </el-table-column>
                    <el-table-column prop="SignLimit" sortable label="登入数" width="90">
                        <template #default="scope">
                            <p>已用 : {{scope.row.SignCount}}</p>
                            <p>总量 : {{scope.row.SignLimit == -1 ?'//无限':scope.row.SignLimit}}</p>
                        </template>
                    </el-table-column>
                    <el-table-column prop="Access" label="本机权限" width="90">
                        <template #default="scope">
                            <el-dropdown size="small" style="margin-top:.4rem" @command="handleAccessCommand">
                                <span class="el-dropdown-link" style="font-size:1.2rem">
                                    <span>{{scope.row.Access.toString(16).toLocaleUpperCase()}}</span><span class="table-icon"><el-icon class="el-icon--right"><arrow-down /></el-icon></span>
                                </span>
                                <template #dropdown>
                                    <el-dropdown-menu>
                                        <template v-for="(item) in state.accesss" :key="item.value">
                                            <template v-if="shareData.serverAccessHas(scope.row.Access,item.value)">
                                                <el-dropdown-item :icon="Select" :command="{item,row:scope.row}">{{item.text}}</el-dropdown-item>
                                            </template>
                                            <template v-else>
                                                <el-dropdown-item :command="{item,row:scope.row}">{{item.text}}</el-dropdown-item>
                                            </template>
                                        </template>
                                    </el-dropdown-menu>
                                </template>
                            </el-dropdown>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
    </div>
</template>
<script>
import { reactive } from '@vue/reactivity'
import { computed, onMounted } from '@vue/runtime-core'
import { Select } from '@element-plus/icons'
import { ElMessage } from 'element-plus'
import { getPage, update } from '../../../apis/users'
import { shareData } from '../../../states/shareData'
import plugin from './plugin'
import settingPlugin from '../../server/settings/plugin'
export default {
    plugin: Object.assign(JSON.parse(JSON.stringify(plugin)), { access: settingPlugin.access }),
    components: { Select },
    setup() {

        const state = reactive({
            loading: false,
            account: '',
            sort: 0,
            accesss: computed(() => {
                return Object.keys(shareData.clientAccess).map(key => {
                    return shareData.clientAccess[key];
                });
            }),
            data: {
                Page: 1,
                PageSize: 10,
                Count: 0,
                Data: [],
            }
        });

        const getData = () => {
            state.loading = true;
            getPage({
                Page: state.data.Page, PageSize: state.data.PageSize,
                account: state.account,
                sort: state.sort
            }).then((res) => {
                if (res) {
                    let json = new Function(`return ${res}`)();
                    if (json.Data) {
                        state.data = json;
                    }
                }
                state.loading = false;
            }).catch(() => {
                state.loading = false;
            });
        };
        onMounted(() => {
            getData();
        });

        const handleAccessCommand = (command) => {
            let access = command.row.Access;
            if (shareData.serverAccessHas(command.row.Access, command.item.value)) {
                command.row.Access = shareData.serverAccessDel(command.row.Access, command.item.value);
            } else {
                command.row.Access = shareData.serverAccessAdd(command.row.Access, command.item.value);
            }
            let json = JSON.parse(JSON.stringify(command.row));
            update(json).then((msg) => {
                if (msg) {
                    ElMessage.error(msg);
                    command.row.Access = access;
                } else {
                    ElMessage.success('操作成功');
                }
            }).catch(() => {
                ElMessage.error('操作失败');
                command.row.Access = access;
            });
        }

        const handleSort = ({ column, prop, order }) => {
            const sortField = {
                'ID': 1,
                'AddTime': 2,
                'EndTime': 4,
                'NetFlow': 8,
                'SignLimit': 16
            };
            const types = {
                'descending': 1,
                'ascending': 0,
            }
            if (order == null) {
                state.sort = 0;
            } else {
                state.sort = sortField[prop] | types[order] << 7;
            }
            getData();
        }

        return {
            state, getData, Select, shareData, handleAccessCommand, handleSort
        }
    }
}
</script>
<style lang="stylus" scoped>
@media screen and (max-width: 500px) {
    .el-col-24 {
        max-width: 100%;
        flex: 0 0 100%;
    }
}

.table-icon {
    vertical-align: middle;
    margin-top: -4px;
}

.forward-wrap {
    padding: 2rem;

    .inner {
        border: 1px solid var(--main-border-color);
        border-radius: 0.4rem;
        background-color: #fff;
    }

    .head {
        padding: 1rem;
        line-height: 2.5rem;
        border-bottom: 1px solid var(--main-border-color);
    }

    .content {
        padding: 1rem;
    }
}
</style>