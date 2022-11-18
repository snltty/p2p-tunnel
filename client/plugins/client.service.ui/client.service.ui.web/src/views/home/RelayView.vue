<!--
 * @Author: snltty
 * @Date: 2022-11-08 09:57:59
 * @LastEditors: snltty
 * @LastEditTime: 2022-11-18 15:30:02
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\RelayView.vue
-->
<template>
    <el-dialog title="选择中继线路" v-model="state.show" draggable center :close-on-click-modal="false" top="1rem" append-to-body width="35rem">
        <el-alert title="只展示数据可连通线路" description="选择一个你喜欢的线路" show-icon type="info" effect="dark" :closable="false" />
        <ul>
            <template v-for="(item,index) in state.paths" :key="index">
                <li>
                    <dl>
                        <dt class="flex">
                            <Signal :value="item.delay"></Signal>
                            <span class="flex-1"></span>
                            <div>
                                <el-button size="small" @click="handleSelect(item.path)">选择此线路</el-button>
                            </div>
                        </dt>
                        <dd>
                            <template v-for="(p,pi) in item.pathName" :index="pi">
                                <span class="label" v-if="pi>0">
                                    &lt;--&gt;
                                </span>
                                <strong>{{p}}</strong>
                            </template>
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
import { getDelay, getConnects } from '../../apis/clients'
import { injectClients } from '../../states/clients'
import { injectRegister } from '../../states/register'
import Signal from './Signal.vue'
export default {
    props: ['modelValue'],
    emits: ["update:modelValue", 'success'],
    components: { Signal },
    setup (props, { emit }) {

        const shareData = inject('share-data');
        const clientsState = injectClients();
        const registerState = injectRegister();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            connects: {},
            start: registerState.RemoteInfo.ConnectId,
            end: shareData.toId,
            paths: []

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
                return {
                    name: c.Name,
                    id: c.Id,
                    delay: state.delays[c.Id] || -1
                }
            });
        });

        let timer = 0;
        const getData = () => {
            getConnects().then((connects) => {

                let _connects = [];
                for (let j in connects) {
                    _connects.push({
                        Id: +j,
                        Connects: connects[j]
                    })
                }
                state.connects = _connects;
                let starts = _connects.filter(c => c.Connects.filter(c => c == state.start).length > 0 && c.Connects.length > 0);
                let paths = fun(starts, [state.start], [state.start], []);
                //服务器开启了中继
                if (registerState.RemoteInfo.Relay) {
                    paths.push([state.start, 0, state.end]);
                }
                if (paths.length > 0) {
                    getDelay(paths).then((delays) => {

                        let clients = clientsState.clients.reduce((json, current, index) => {
                            json[current.Id] = current;
                            return json;
                        }, {});
                        state.paths = paths.map((path, index) => {
                            return {
                                delay: delays[index],
                                path: path,
                                pathName: path.map(c => {
                                    if (c == state.start) return registerState.ClientConfig.Name;
                                    else if (c == 0) return '服务器';
                                    return clients[c].Name;
                                })
                            }
                        });
                        console.log(JSON.stringify(state.paths));
                        timer = setTimeout(getData, 1000);
                    }).catch((e) => {
                        timer = setTimeout(getData, 1000);
                    });
                } else {
                    timer = setTimeout(getData, 1000);
                }
            }).catch(() => {
                timer = setTimeout(getData, 1000);
            });
        }
        const fun = (starts, exclude = [], path = [], result = []) => {

            for (let i = 0; i < starts.length; i++) {
                if (starts[i].Id == state.end) {
                    path.push(starts[i].Id);
                    if (path[0] == state.start) {
                        result.push(path);
                    }
                    continue;
                }

                let _exclude = exclude.slice(0);
                _exclude.push(starts[i].Id);
                let _path = path.slice(0);
                _path.push(starts[i].Id);

                let lastIds = starts[i].Connects.filter(c => _exclude.indexOf(c) == -1);
                let last = state.connects.filter(c => lastIds.indexOf(c.Id) >= 0);
                if (last.length > 0) {
                    fun(last, _exclude, _path, result);
                } else {
                    if (_path[_path.length - 1] == state.end) {
                        result.push(_path);
                    }
                }
            }
            return result;
        }
        onMounted(() => {
            getData();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        });
        const handleSelect = (path) => {
            emit('success', path);
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
    padding: 1rem 0;

    dl
        border: 1px solid #eee;
        border-radius: 0.4rem;

        dt
            border-bottom: 1px solid #eee;
            padding: 1rem;
            font-size: 1.4rem;
            font-weight: 600;
            color: #555;
            line-height: 2.4rem;

        dd
            cursor: pointer;
            padding: 0.4rem 1rem;

            span.label
                width: 4rem;
</style>