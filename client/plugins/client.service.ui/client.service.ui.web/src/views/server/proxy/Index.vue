<template>
    <div class="proxy-setting-wrap flex flex-column h-100">
        <div class="inner">
            <div class="head flex">
                <el-button size="small" :loading="state.loading" @click="Add">添加新项</el-button>
                <span class="split"></span>
                <el-select v-model="state.proxys" size="small" multiple collapse-tags collapse-tags-tooltip style="width: 220px" @change="loadData">
                    <el-option v-for="item in state.clientProxys" :key="item.value" :label="item.text" :value="item.value" />
                </el-select>
                <span class="split"></span>
                <el-button size="small" :loading="state.loading" @click="loadData">刷新列表</el-button>
                <el-popover placement="top-start" title="说明" :width="300" trigger="hover" content="【ipv6】局域网全部阻止，【ipv4】所有ip验证黑名单，局域网ip验证白名单，【域名】全部通过">
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
                    <el-table border stripe :data="state.data" size="small" :row-class-name="rowClassName">
                        <el-table-column type="index" width="40" />
                        <el-table-column prop="protocol" label="协议"></el-table-column>
                        <el-table-column prop="text" label="作用域"></el-table-column>
                        <el-table-column prop="Port" label="端口"></el-table-column>
                        <el-table-column prop="IP" label="ip">
                            <template #default="scope">
                                <p v-for="(item,index) in scope.row.IP" :key="index">{{item}}</p>
                            </template>
                        </el-table-column>
                        <el-table-column prop="Remark" label="备注"></el-table-column>
                        <el-table-column fixed="right" label="op" width="90">
                            <template #default="scope">
                                <el-button size="small" link @click="handleEdit(scope.row)">编辑</el-button>
                                <el-popconfirm title="删除不可逆，是否确认?" @confirm="handleDel(scope.row)">
                                    <template #reference>
                                        <el-button link size="small">删除</el-button>
                                    </template>
                                </el-popconfirm>
                            </template>
                        </el-table-column>
                    </el-table>
                </div>
                <!-- <el-empty v-else /> -->
            </div>
        </div>
        <Add v-if="state.showAdd" v-model="state.showAdd" @success="loadData"></Add>
    </div>
</template>

<script>
import { reactive, ref } from '@vue/reactivity'
import { get, remove } from '../../../apis/proxy-server'
import { computed, onMounted, provide } from '@vue/runtime-core'
import { shareData } from '../../../states/shareData'
import Add from './Add.vue'
import plugin from './plugin'
import settingPlugin from '../settings/plugin'
export default {
    plugin: Object.assign(JSON.parse(JSON.stringify(plugin)), { access: settingPlugin.access }),
    components: { Add },
    setup() {

        const clientProxys = computed(() => [{ text: '全局', value: 0 }].concat(JSON.parse(JSON.stringify(shareData.clientProxys))));
        const allproxyMaps = computed(() => clientProxys.value.reduce((value, current) => {
            value[current.value] = current.text;
            return value;
        }, {}));

        const state = reactive({
            loading: false,
            showAdd: false,
            proxys: [],
            data: [],
            protocol: { 1: 'TCP', 2: 'UDP', 3: 'TCP/UDP' },
            types: { 0: '允许', 1: '阻止' },
            allproxys: computed(() => clientProxys.value.map(c => c.value)),
            allproxyMaps: allproxyMaps,
            clientProxys: clientProxys
        });
        const addData = ref({ ID: 0 });
        provide('add-data', addData);

        const Add = () => {
            addData.value = { ID: 0 };
            state.showAdd = true;
        }
        const handleEdit = (row) => {
            addData.value = row;
            state.showAdd = true;
        }
        const handleDel = (row) => {
            state.loading = true;
            remove(row.ID).then((res) => {
                state.loading = false;
                loadData();
            }).catch(() => {
                state.loading = false;
            });
        }
        const loadData = () => {
            state.loading = true;
            get().then((res) => {
                state.loading = false;
                if (state.proxys.length > 0) {
                    res.Firewall = res.Firewall.filter(c => state.proxys.indexOf(c.PluginId) >= 0);
                }
                res.Firewall = res.Firewall.filter(c => state.allproxys.indexOf(c.PluginId) >= 0).sort((a, b) => a.PluginId - b.PluginId);
                res.Firewall.forEach(c => {
                    c.text = allproxyMaps.value[c.PluginId];
                    c.protocol = state.protocol[c.Protocol];
                })
                state.data = res.Firewall;
            }).catch(() => {
                state.loading = false;
            });
        }
        const rowClassName = ({ row }) => {
            return `type-${row.Type}`;
        }
        onMounted(() => {
            loadData();
        });

        return {
            shareData, state, Add, handleEdit, handleDel, loadData, rowClassName
        }
    }
}
</script>
<style lang="stylus">
.proxy-setting-wrap {
    tr.type-0 {
        td:nth-child(3) {
            color: green;
        }
    }

    tr.type-1 {
        td:nth-child(3) {
            color: red;
            text-decoration: line-through;
        }
    }
}
</style>
<style lang="stylus" scoped>
.proxy-setting-wrap {
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