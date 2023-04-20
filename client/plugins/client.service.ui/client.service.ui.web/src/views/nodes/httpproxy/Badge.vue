<template>
    <el-dropdown :hide-on-click="false" @visible-change="loadData">
        <span class="el-dropdown-link">
            <a href="javascript:;" :class="{active:isTarget}">http代理</a>
            <el-icon>
                <arrow-down />
            </el-icon>
        </span>
        <template #dropdown>
            <el-dropdown-menu>
                <el-dropdown-item v-if="isTarget">
                    <el-button size="small" :loading="state.loading" @click="handleClear">移除目标</el-button>
                </el-dropdown-item>
                <el-dropdown-item v-else>
                    <el-button size="small" :loading="state.loading" @click="handleSet" title="将此客户端端设为http代理目标端">设为目标</el-button>
                </el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
</template>

<script>
import { getConfigure, saveConfigure } from '../../../apis/configure'
import { reactive } from '@vue/reactivity'
import { computed, onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['params'],
    setup(props) {
        const name = props.params.Name;
        const isTarget = computed(() => name == state.TargetName);
        const state = reactive({
            loading: false,
            TargetName: ''
        });
        const loadData = () => {
            getConfigure(plugin.config).then((res) => {
                let json = new Function(`return ${res}`)();
                state.TargetName = json.TargetName;
            }).catch(() => {

            });
        }
        onMounted(() => {
            loadData();
        });

        const handleSubmit = () => {
            state.loading = true;
            getConfigure(plugin.config).then((res) => {
                let json = new Function(`return ${res}`)();
                json.TargetName = state.TargetName;
                saveConfigure(plugin.config, JSON.stringify(json)).then((res) => {
                    state.loading = false;
                    loadData();
                }).catch(() => {
                    state.loading = false;
                });
            }).catch(() => {
                state.loading = false;
            });
        }

        const handleSet = (name) => {
            state.TargetName = name;
            handleSubmit();
        }
        const handleClear = () => {
            state.TargetName = '';
            handleSubmit();
        }

        return {
            state, isTarget, loadData, handleSet, handleClear
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-dropdown {
    font-size: 1.3rem;
    height: 1.4rem;
    overflow: hidden;
    padding-top: 1px;

    a {
        color: #666;
        vertical-align: top;

        &.active {
            color: green;
        }
    }

    .el-icon {
        vertical-align: middle;
    }
}
</style>
