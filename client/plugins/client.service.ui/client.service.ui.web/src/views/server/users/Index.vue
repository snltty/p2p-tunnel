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
                <el-table :data="state.data.Data" stripe border size="small">
                    <el-table-column prop="ID" label="ID" width="50" />
                    <el-table-column prop="Account" label="账号">
                        <template #default="scope">
                            <el-dropdown size="small" style="margin-top:.4rem">
                                <span class="el-dropdown-link" style="font-size:1.2rem">
                                    <span>{{scope.row.Account}}</span> <span class="table-icon"><el-icon class="el-icon--right table-icon"><arrow-down /></el-icon></span>
                                </span>
                                <template #dropdown>
                                    <el-dropdown-menu>
                                        <el-dropdown-item>
                                            <div style="padding:1rem 0" @click.stop>
                                                <el-input size="small" v-model="scope.row.state.password" style="width:10rem;margin-right:.4rem"></el-input>
                                                <el-button size="small">新密码</el-button>
                                            </div>
                                        </el-dropdown-item>
                                    </el-dropdown-menu>
                                </template>
                            </el-dropdown>
                        </template>
                    </el-table-column>
                    <el-table-column prop="EndTime" label="结束时间" width="160">
                        <template #default="scope">
                            <el-dropdown size="small" style="margin-top:.4rem">
                                <span class="el-dropdown-link" style="font-size:1.2rem">
                                    <span>{{scope.row.EndTime}}</span> <span class="table-icon"><el-icon class="el-icon--right table-icon"><arrow-down /></el-icon></span>
                                </span>
                                <template #dropdown>
                                    <el-dropdown-menu>
                                        <el-dropdown-item>
                                            <div style="padding:1rem 0" @click.stop>
                                                <el-date-picker v-model="scope.row.state.endTime" type="datetime" size="small" :clearable="false" style="width:15rem;margin-right:1.6rem" />
                                                <el-button size="small">设置</el-button>
                                            </div>
                                        </el-dropdown-item>
                                        <el-dropdown-item>
                                            <div @click.stop>
                                                <el-input size="small" v-model="scope.row.state.addYear" style="width:4rem"></el-input>年
                                                <el-input size="small" v-model="scope.row.state.addMonth" style="width:4rem"></el-input>月
                                                <el-input size="small" v-model="scope.row.state.addDate" style="width:4rem"></el-input>日
                                            </div>
                                        </el-dropdown-item>
                                        <el-dropdown-item>
                                            <div style="padding-bottom:1rem" @click.stop>
                                                <el-input size="small" v-model="scope.row.state.addHour" style="width:4rem"></el-input>时
                                                <el-input size="small" v-model="scope.row.state.addMinute" style="width:4rem"></el-input>分
                                                <el-input size="small" v-model="scope.row.state.addSecond" style="width:4rem"></el-input>秒
                                                <el-button size="small">增加</el-button>
                                            </div>
                                        </el-dropdown-item>
                                    </el-dropdown-menu>
                                </template>
                            </el-dropdown>
                        </template>
                    </el-table-column>
                    <el-table-column prop="NetFlow" label="剩余流量" width="70">
                        <template #default="scope">
                            <el-dropdown size="small" style="margin-top:.4rem">
                                <span class="el-dropdown-link" style="font-size:1.2rem">
                                    <span>{{scope.row.NetFlow == -1 ?'-':scope.row.NetFlow.sizeFormat().join('')}}</span> <span class="table-icon"><el-icon class="el-icon--right table-icon"><arrow-down /></el-icon></span>
                                </span>
                                <template #dropdown>
                                    <el-dropdown-menu>
                                        <el-dropdown-item>
                                            <div style="padding:1rem 0" @click.stop>
                                                <el-input size="small" v-model="scope.row.state.netFlow" style="width:6rem;margin-right:.4rem"></el-input>
                                                <el-button size="small">设置</el-button>
                                                <span>-1无限制，单位B</span>
                                            </div>
                                        </el-dropdown-item>
                                        <el-dropdown-item>
                                            <div style="padding-bottom:1rem" @click.stop>
                                                <el-input size="small" v-model="scope.row.state.addTB" style="width:4rem"></el-input>TB
                                                <el-input size="small" v-model="scope.row.state.addGB" style="width:4rem"></el-input>GB
                                                <el-input size="small" v-model="scope.row.state.addMB" style="width:4rem"></el-input>MB
                                                <el-button size="small">增加</el-button>
                                            </div>
                                        </el-dropdown-item>
                                    </el-dropdown-menu>
                                </template>
                            </el-dropdown>
                        </template>
                    </el-table-column>
                    <el-table-column prop="SignLimit" label="登录限制" width="70">
                        <template #default="scope">
                            <el-dropdown size="small" style="margin-top:.4rem">
                                <span class="el-dropdown-link" style="font-size:1.2rem">
                                    <span>{{scope.row.SignLimit == 0 ?'-':scope.row.SignLimit}}</span> <span class="table-icon"><el-icon class="el-icon--right table-icon"><arrow-down /></el-icon></span>
                                </span>
                                <template #dropdown>
                                    <el-dropdown-menu>
                                        <el-dropdown-item>
                                            <div style="padding:1rem 0" @click.stop>
                                                <el-input size="small" v-model="scope.row.state.signLimit" style="width:6rem;margin-right:.4rem"></el-input>
                                                <el-button size="small">设置</el-button>
                                            </div>
                                        </el-dropdown-item>
                                        <el-dropdown-item>0无限制</el-dropdown-item>
                                    </el-dropdown-menu>
                                </template>
                            </el-dropdown>
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
                            <el-button size="small" type="danger">
                                <el-icon>
                                    <Delete />
                                </el-icon>
                            </el-button>
                        </template>
                    </el-table-column>
                </el-table>
            </div>
        </div>
        <Add v-if="state.showAdd" v-model="state.showAdd" @success="getData"></Add>
    </div>
</template>
<script>
import { reactive, ref } from '@vue/reactivity'
import { getPage } from '../../../apis/users-server'
import { computed, onMounted, provide } from '@vue/runtime-core'
import Add from './Add.vue'
import { shareData } from '../../../states/shareData'
import { Select } from '@element-plus/icons'
export default {
    service: 'ServerUdpForwardClientService',
    components: { Add, Select },
    setup() {

        const state = reactive({
            loading: false,
            account: '',
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

        });

        const getData = () => {
            getPage(state.data.Page, state.data.PageSize).then((res) => {
                let json = new Function(`return ${res}`)();
                json.Data.forEach(c => {
                    c.state = {
                        addTB: 0,
                        addGB: 0,
                        addMB: 0,

                        addYear: 0,
                        addMonth: 0,
                        addDate: 0,
                        addHour: 0,
                        addMinute: 0,
                        addSecond: 0,

                        netFlow: c.NetFlow,
                        signLimit: c.SignLimit,
                        endTime: c.EndTime,
                        password: c.Password,
                    }
                });
                state.data = json;
            });
        };
        onMounted(() => {
            getData();
        });

        const handleAccessCommand = (command) => {
            if (shareData.serverAccessHas(command.row.Access, command.item.value)) {
                command.row.Access = shareData.serverAccessDel(command.row.Access, command.item.value);
            } else {
                command.row.Access = shareData.serverAccessAdd(command.row.Access, command.item.value);
            }
        }

        const addData = ref({ ID: 0 });
        provide('add-data', addData);
        const handleAdd = () => {
            addData.value = { ID: 0 };
            state.showAdd = true;
        }

        return {
            state, getData, Select, shareData, handleAccessCommand, handleAdd
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