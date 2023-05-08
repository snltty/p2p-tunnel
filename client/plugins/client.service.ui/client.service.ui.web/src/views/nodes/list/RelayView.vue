<template>
    <el-dialog title="选择中继线路" v-model="state.show" draggable center :close-on-click-modal="false" top="1vh" width="50rem">
        <ul class="nodes-ul scrollbar">
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
                            <template v-for="(p,pi) in item.pathName" :key="pi">
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
import { getDelay, getConnects } from '../../../apis/clients'
import { injectClients } from '../../../states/clients'
import { injectSignIn } from '../../../states/signin'
import Signal from '../../../components/Signal.vue'
import plugin from './plugin'
export default {
    plugin: plugin,
    props: ['modelValue'],
    emits: ["update:modelValue", 'success'],
    components: { Signal },
    setup(props, { emit }) {

        const shareData = inject('share-data');
        const clientsState = injectClients();
        const signinState = injectSignIn();
        const state = reactive({
            show: props.modelValue,
            loading: false,
            connects: {},
            start: signinState.RemoteInfo.ConnectId,
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
            }]).filter(c => state.delays[c.ConnectionId]).map(c => {
                return {
                    name: c.Name,
                    id: c.ConnectionId,
                    delay: state.delays[c.ConnectionId] || -1
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
                let starts = _connects.filter(c => c.Connects.filter(c => c == state.start).length > 0 && c.Connects.length > 1);
                let paths = fun(starts, [state.start], [state.start], []);
                //服务器开启了中继
                if (signinState.RemoteInfo.Relay) {
                    paths.push([state.start, 0, state.end]);
                }
                //直连的不要
                paths = paths.filter(c => c.length > 2);
                if (paths.length > 0) {
                    getDelay(paths).then((delays) => {

                        let clients = clientsState.clients.reduce((json, current, index) => {
                            json[current.ConnectionId] = current;
                            return json;
                        }, {});

                        let clients1 = clientsState.clients.reduce((json, current, index) => {
                            json[current.ConnectionId] = current.Name;
                            return json;
                        }, {});

                        state.paths = paths.map((path, index) => {
                            return {
                                delay: delays[index],
                                path: path,
                                pathName: path.map(c => {
                                    if (c == state.start) return signinState.ClientConfig.Name;
                                    else if (c == 0) return '服务器';
                                    return clients[c].Name;
                                })
                            }
                        });
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
                let _path = path.slice(0);
                if (starts[i].ConnectionId == state.end) {
                    _path.push(starts[i].ConnectionId);
                    if (_path[0] == state.start) {
                        result.push(_path);
                    }
                    continue;
                }

                let _exclude = exclude.slice(0);
                _exclude.push(starts[i].ConnectionId);
                _path.push(starts[i].ConnectionId);

                let lastIds = starts[i].Connects.filter(c => _exclude.indexOf(c) == -1);
                let last = state.connects.filter(c => lastIds.indexOf(c.ConnectionId) >= 0);
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
.nodes-ul {
    min-height: 10rem;
    max-height: 50rem;
    border: 1px solid #eee;
    padding: 0.6rem;
    border-radius: 0.4rem;
}

li {
    padding: 1rem 0;

    dl {
        border: 1px solid #eee;
        border-radius: 0.4rem;

        dt {
            border-bottom: 1px solid #eee;
            padding: 1rem;
            font-size: 1.4rem;
            font-weight: 600;
            color: #555;
            line-height: 2.4rem;
        }

        dd {
            cursor: pointer;
            padding: 0.4rem 1rem;

            span.label {
                width: 4rem;
            }
        }
    }
}
</style>