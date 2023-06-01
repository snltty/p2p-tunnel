<template>
    <el-dropdown :hide-on-click="false" @visible-change="loadData">
        <span class="el-dropdown-link">
            <a href="javascript:;" :class="{active:isTarget}">socks5</a>
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
                    <el-button size="small" :loading="state.loading" @click="handleSet" title="将此节点设为socks5代理目标端">设为目标</el-button>
                </el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
</template>

<script>
import { get, set } from '../../../../apis/socks5'
import { reactive } from '@vue/reactivity'
import { computed, onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['params'],
    setup(props) {
        const id = props.params.ConnectionId;

        const isTarget = computed(() => id == state.targetConnectionId);
        const state = reactive({
            loading: false,
            targetConnectionId: 0
        });
        const loadData = () => {
            get().then((res) => {
                state.targetConnectionId = res.TargetConnectionId;
            }).catch(() => {

            });
        }
        onMounted(() => {
            loadData();
        });

        const handleSubmit = () => {
            state.loading = true;
            get().then((res) => {
                res.TargetConnectionId = state.targetConnectionId;
                set(res).then((res) => {
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
            state.targetConnectionId = id;
            handleSubmit();
        }
        const handleClear = () => {
            state.targetConnectionId = 0;
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