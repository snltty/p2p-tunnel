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
import { getListProxy, addListen } from '../../../apis/forward'
import { reactive } from '@vue/reactivity'
import { computed, onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['params'],
    setup(props) {
        const name = props.params.Name;
        const isTarget = computed(() => name == state.targetName);
        const state = reactive({
            loading: false,
            targetName: ''
        });
        const loadData = () => {
            getListProxy().then((res) => {
                state.targetName = res.Name;
            }).catch(() => {

            });
        }
        onMounted(() => {
            loadData();
        });

        const handleSubmit = () => {
            state.loading = true;
            getListProxy().then((res) => {
                res.Name = state.targetName;
                addListen(res).then((res) => {
                    state.loading = false;
                    loadData();
                }).catch(() => {
                    state.loading = false;
                });
            }).catch(() => {
                state.loading = false;
            });
        }

        const handleSet = () => {
            state.targetName = name;
            handleSubmit();
        }
        const handleClear = () => {
            state.targetName = '';
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
