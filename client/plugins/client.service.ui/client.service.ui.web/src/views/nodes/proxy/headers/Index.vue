<template>
    <div class="httpheaders-setting-wrap flex flex-column">
        <div class="inner">
            <div class="head flex">
                <el-button type="primary" size="small" :loading="state.loading" @click="submit">保存更改</el-button>
                <el-button size="small" :loading="state.loading" @click="loadData">刷新列表</el-button>
                <el-popover placement="top-start" title="说明" :width="300" trigger="hover" content="为http协议添加请求头，内容将以类似cookie的方式放在名为Snltty-Kvs的key中">
                    <template #reference>
                        <el-icon>
                            <Warning />
                        </el-icon>
                    </template>
                </el-popover>
                <span class="flex-1"></span>
            </div>
            <div class="body flex-1 relative">
                <div>
                    <el-table border stripe :data="state.data" size="small">
                        <el-table-column type="index" width="40" />
                        <el-table-column prop="text" label="作用域" width="120"></el-table-column>
                        <el-table-column prop="dynamic" label="动态" width="160">
                            <template #default="scope">
                                <ul>
                                    <template v-for="(item,index) in scope.row.dynamics" :key="index">
                                        <li class="flex">
                                            <el-checkbox v-model="item.checked" size="small">{{item.text}}</el-checkbox>
                                            <el-input class="key-name" size="small" v-model="item.key" disabled />
                                        </li>
                                    </template>
                                </ul>
                            </template>
                        </el-table-column>
                        <el-table-column prop="static" label="静态">
                            <template #default="scope">
                                <ul>
                                    <template v-for="(item,index) in scope.row.statics" :key="index">
                                        <li class="flex">
                                            <el-input class="key-name" size="small" v-model="item.key" placeholder="key" />
                                            :
                                            <el-input class="value-name" size="small" v-model="item.value" placeholder="value" />
                                            <el-button type="danger" icon="Delete" link @click="handleDelStatic(scope.row,index)"></el-button>
                                            <el-button type="success" icon="CirclePlus" link @click="handleAddStatic(scope.row,index)"></el-button>
                                        </li>
                                    </template>
                                </ul>
                            </template>
                        </el-table-column>
                    </el-table>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import { reactive } from '@vue/reactivity'
import { get, setHeaders } from '../../../../apis/proxy'
import { shareData } from '../../../../states/shareData'
import { onMounted } from '@vue/runtime-core'
import plugin from './plugin'
import { ElMessage } from 'element-plus'
export default {
    plugin: plugin,
    components: {},
    setup() {

        const clientProxys = shareData.clientProxys.filter(c => c.local).map(c => {
            return { text: c.text, pluginId: c.value };
        }).map(c => {
            c.dynamics = [
                { text: '节点ip', value: 1, key: 'ip', checked: false },
                { text: '节点名', value: 2, key: 'node', checked: false }
            ];
            c.statics = [{ key: '', value: '' }]
            return c;
        });

        const handleDelStatic = (row, index) => {
            if (row.statics.length == 1) {
                row.statics[0] = { key: '', value: '' };
            } else {
                row.statics.splice(index, 1);
            }
        }
        const handleAddStatic = (row, index) => {
            row.statics.splice(index + 1, 0, { key: '', value: '' });
        }

        const state = reactive({
            loading: false,
            data: clientProxys,
            clientProxys: clientProxys
        });
        const loadData = () => {
            state.loading = true;
            get().then((res) => {
                console.log(res);
                state.loading = false;
                const headers = res.HttpHeader;
                if (headers.length > 0) {
                    let data = JSON.parse(JSON.stringify(clientProxys));
                    for (let i = 0; i < headers.length; i++) {
                        const header = headers[i];
                        const proxy = data.filter(c => c.pluginId == header.PluginId)[0];
                        if (proxy) {
                            proxy.pluginId = header.PluginId;
                            proxy.dynamics.forEach(item => {
                                item.checked = (header.Dynamics & item.value) == item.value;
                            });
                            proxy.statics = Object.keys(header.Statics).reduce((value, item, index) => {
                                value.push({ key: item, value: header.Statics[item] });
                                return value;
                            }, []);
                            if (proxy.statics.length == 0) {
                                proxy.statics = [{ key: '', value: '' }];
                            }
                        }
                    }
                    state.data = data;
                }
            }).catch(() => {
                state.loading = false;
            });
        }
        onMounted(() => {
            loadData();
        });

        const submit = () => {
            const data = state.data.map(item => {
                return {
                    pluginId: item.pluginId,
                    dynamics: item.dynamics.reduce((value, item, index) => {
                        if (item.checked) {
                            value |= item.value;
                        }
                        return value;
                    }, 0),
                    statics: item.statics.filter(c => c.key && c.value).reduce((value, item, index) => {
                        value[item.key] = item.value;
                        return value;
                    }, {})
                }
            });
            state.loading = true;
            setHeaders(data).then((res) => {
                state.loading = false;
                if (res) {
                    ElMessage.success('操作成功!');
                } else {
                    ElMessage.error('操作失败!');
                }
            }).catch(() => {
                state.loading = false;
            })
        }

        return {
            shareData, state, submit, loadData, handleDelStatic, handleAddStatic
        }
    }
}
</script>
el-input__wrapper
<style lang="stylus">
.httpheaders-setting-wrap {
    .el-input {
        .el-input__wrapper {
            background-color: transparent;
            box-shadow: none;
            border-bottom: 1px solid #ddd;
            border-radius: 0;
        }
    }
}
</style>
<style lang="stylus" scoped>
.httpheaders-setting-wrap {
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

    .key-name {
        width: 7rem;
    }

    .value-name {
        width: 12rem;
    }
}
</style>