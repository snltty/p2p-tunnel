<template>
    <div class="forward-wrap">
        <div class="inner">
            <div class="head flex">
                <el-button type="primary" size="small" @click="handleAdd">增加账号</el-button>
                <el-input v-model="state.account" size="small" style="width:10rem;margin:0 .4rem 0 1rem"></el-input>
                <el-button size="small" @click="getData">刷新列表</el-button>
                <span class="flex-1"></span>
            </div>
            <div class="content">
                <el-table :data="state.data.Data" stripe border size="small" @sort-change="handleSort">
                    <el-table-column prop="ID" sortable label="账号">
                        <template #default="scope">
                            <a href="javascript:;" @click="handleAccount(scope.row)">【{{scope.row.ID}}】{{scope.row.Account}}</a>
                        </template>
                    </el-table-column>
                    <el-table-column prop="EndTime" sortable label="时间" width="140">
                        <template #default="scope">
                            <p>{{scope.row.AddTime}}</p>
                            <p><a href="javascript:;" @click="handleEndTime(scope.row)">{{scope.row.EndTime}}</a></p>
                        </template>
                    </el-table-column>
                    <el-table-column prop="NetFlow" sortable label="流量">
                        <template #default="scope">
                            <a href="javascript:;" @click="handleNetFlow(scope.row)">{{scope.row.NetFlow == -1 ?'//无限制' :scope.row.NetFlow.sizeFormat().join('')}}</a>
                        </template>
                    </el-table-column>
                    <el-table-column prop="SignLimit" sortable label="登入数" width="90">
                        <template #default="scope">
                            <a href="javascript:;" @click="handleSignLimit(scope.row)">{{scope.row.SignLimit == 0 ?'//无限制':scope.row.SignLimit}}</a>
                        </template>
                    </el-table-column>
                    <el-table-column prop="Access" label="权限" width="90">
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
                    <el-table-column fixed="right" width="55">
                        <template #default="scope">
                            <el-popconfirm title="删除不可逆，是否确认?" @confirm="handleDelete(scope.row)">
                                <template #reference>
                                    <el-button size="small" type="danger">
                                        <el-icon>
                                            <Delete />
                                        </el-icon>
                                    </el-button>
                                </template>
                            </el-popconfirm>

                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
        <Add v-if="state.showAdd" v-model="state.showAdd" @success="getData"></Add>
        <Password v-if="state.showAccount" v-model="state.showAccount" @success="getData"></Password>
        <EndTime v-if="state.showEndTime" v-model="state.showEndTime" @success="getData"></EndTime>
        <NetFlow v-if="state.showNetFlow" v-model="state.showNetFlow" @success="getData"></NetFlow>
        <SignLimit v-if="state.showSignLimit" v-model="state.showSignLimit" @success="getData"></SignLimit>
    </div>
</template>
<script>
import { reactive, ref } from '@vue/reactivity'
import { computed, onMounted, provide } from '@vue/runtime-core'
import { Select } from '@element-plus/icons'
import { ElMessage } from 'element-plus'
import { getPage, add, remove } from '../../../apis/users-server'
import { shareData } from '../../../states/shareData'
import Add from './Add.vue'
import Password from './Password.vue'
import EndTime from './EndTime.vue'
import NetFlow from './NetFlow.vue'
import SignLimit from './SignLimit.vue'

export default {
    service: 'ServerUdpForwardClientService',
    components: { Add, Password, EndTime, NetFlow, SignLimit, Select },
    setup() {

        const state = reactive({
            loading: false,
            account: '',
            sort: 0,
            accesss: computed(() => {
                return Object.keys(shareData.serverAccess).map(key => {
                    return shareData.serverAccess[key];
                });
            }),
            data: {
                Page: 1,
                PageSize: 10,
                Count: 0,
                Data: [],
            },
            showAdd: false,
            showAccount: false,
            showEndTime: false,
            showNetFlow: false,
            showSignLimit: false
        });

        const getData = () => {
            state.loading = true;
            getPage({
                Page: state.data.Page, PageSize: state.data.PageSize,
                account: state.account,
                sort: state.sort
            }).then((res) => {
                let json = new Function(`return ${res}`)();
                state.data = json;
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
            add(json).then((msg) => {
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
        const handleAccount = (row) => {
            addData.value = row;
            state.showAccount = true;
        }
        const handleEndTime = (row) => {
            addData.value = row;
            state.showEndTime = true;
        }
        const handleNetFlow = (row) => {
            addData.value = row;
            state.showNetFlow = true;
        }
        const handleSignLimit = (row) => {
            addData.value = row;
            state.showSignLimit = true;
        }
        const handleDelete = (row) => {
            remove(row.ID).then((msg) => {
                if (msg) {
                    ElMessage.error(msg);
                } else {
                    getData();
                    ElMessage.success('操作成功');
                }
            }).catch(() => {
                ElMessage.error('操作失败');
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


        const addData = ref({ ID: 0, NetFlow: 0 });
        provide('add-data', addData);
        const handleAdd = () => {
            addData.value = { ID: 0 };
            state.showAdd = true;
        }

        return {
            state, getData, Select, shareData, handleAccessCommand, handleAdd,
            handleAccount, handleEndTime, handleNetFlow, handleSignLimit, handleDelete, handleSort
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
    }

    .head {
        padding: 1rem;
        border-bottom: 1px solid var(--main-border-color);
    }

    .content {
        padding: 1rem;
    }
}
</style>