<!--
 * @Author: snltty
 * @Date: 2022-11-08 09:57:59
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-13 13:59:38
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\RelayView.vue
-->
<template>
    <el-dialog title="选择中继节点" v-model="state.show" draggable center :close-on-click-modal="false" top="1rem" append-to-body width="35rem">
        <el-alert title="通过某节点与目标节点交换信息的延迟" description="-1为此节点未开启中继，或信息超时" show-icon type="info" effect="dark" :closable="false" />
        <ul>
            <template v-for="(item,index) in clients" :key="index">
                <li>
                    <dl>
                        <dt>{{item.name}}</dt>
                        <dd class="flex">
                            <div class="flex-1 flex"><span class="label">tcp</span>
                                <Signal :value="item.tcp"></Signal>
                            </div>
                            <div class="flex-1 flex"><span class="label">udp</span>
                                <Signal :value="item.udp"></Signal>
                            </div>
                        </dd>
                        <dd class="flex">
                            <span class="flex-1">
                                <el-button size="small" @click="handleSelect(item.id,1)" v-if="item.tcp >= 0">选择</el-button>
                            </span>
                            <span class="flex-1">
                                <el-button size="small" @click="handleSelect(item.id,2)" v-if="item.udp >= 0">选择</el-button>
                            </span>
                        </dd>
                    </dl>
                </li>
            </template>
        </ul>
    </el-dialog>
</template>

<script>
import { computed, reactive } from '@vue/reactivity';
import { inject, onMounted, onUnmounted, watch } from '@vue/runtime-core';
import { getDelay } from '../../apis/clients'
import { injectClients } from '../../states/clients'
import Signal from './Signal.vue'
export default {
    props: ['modelValue'],
    emits: ["update:modelValue", 'success'],
    components: { Signal },
    setup (props, { emit }) {

        const shareData = inject('share-data');
        const clientsState = injectClients();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            delays: {}
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });
        const clients = computed(() => {
            return clientsState.clients.concat([{
                Name: '服务器',
                Id: 0
            }]).filter(c => state.delays[c.Id]).map(c => {
                let delay = state.delays[c.Id] || [-1, -1];
                return {
                    name: c.Name,
                    id: c.Id,
                    tcp: delay[0],
                    udp: delay[1]
                }
            });
        });

        let timer = 0;
        const getData = () => {
            getDelay(shareData.toId).then((res) => {
                state.delays = res;
                timer = setTimeout(getData, 1000);
            }).catch(() => {
                timer = setTimeout(getData, 1000);
            })
        }
        onMounted(() => {
            getData();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        });
        const handleSelect = (id, type) => {
            emit('success', { id: id, type: type, toid: shareData.toId });
            state.show = false;
        }

        return {
            state, clients, handleSelect
        }
    }
}
</script>

<style lang="stylus" scoped>
li
    padding: 1rem 0.6rem;

    dl
        border: 1px solid #eee;
        border-radius: 0.4rem;

        dt
            border-bottom: 1px solid #eee;
            padding: 1rem;
            font-size: 1.4rem;
            font-weight: 600;
            color: #555;

        dd
            cursor: pointer;
            padding: 0.4rem 1rem;

            span.label
                width: 4rem;
</style>