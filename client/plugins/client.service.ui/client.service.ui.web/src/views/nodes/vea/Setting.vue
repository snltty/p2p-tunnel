<template>
    <div class="socks5-wrap">
        <div class="inner">
            <div class="form">
                <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="监听端口" prop="ListenPort">
                                        <el-tooltip class="box-item" effect="dark" content="监听端口，无所谓，填写一个未被占用的端口即可" placement="top-start">
                                            <el-input size="default" v-model="state.form.ListenPort"></el-input>
                                        </el-tooltip>

                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="bufsize" prop="BufferSize">
                                        <el-select size="default" v-model="state.form.BufferSize" placeholder="选择合适的buff">
                                            <el-option v-for="(item,index) in shareData.bufferSizes" :key="index" :label="item" :value="index"></el-option>
                                        </el-select>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="允许访问" prop="ConnectEnable">
                                        <el-tooltip class="box-item" effect="dark" content="作为被访问端时，是否允许访问" placement="top-start">
                                            <el-checkbox v-model="state.form.ConnectEnable" label="开启" />
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                    <el-form-item label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="本机IP" prop="IP">
                                        <el-tooltip class="box-item" effect="dark" content="当前客户端的虚拟网卡ip，各个客户端之间设置不一样的ip，相同网段即可" placement="top-start">
                                            <el-input size="default" v-model="state.form.IP"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="局域网段" prop="LanIPs">
                                        <el-tooltip class="box-item" effect="dark" content="当前客户端的局域网段，各个客户端之间设置不一样的网段即可，192.168.x.0酱紫，为空不启用，多个网段用英文逗号间隔" placement="top-start">
                                            <el-input size="default" v-model="state.form.LanIPs"></el-input>
                                        </el-tooltip>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                </el-form>
            </div>
        </div>
    </div>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity'
import { getConfig, setConfig } from '../../../apis/vea'
import { onMounted } from '@vue/runtime-core'
import { injectClients } from '../../../states/clients'
import { shareData } from '../../../states/shareData'
import plugin from './plugin'
export default {
    plugin: plugin,
    components: {},
    setup() {

        const clientsState = injectClients();
        const targets = computed(() => {
            return clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            });
        });
        const state = reactive({
            configInfo: {},
            form: {
                TargetName: '',
                IP: '',
                LanIPs: '',
                ListenPort: 5415,
                BufferSize: 3,
                ConnectEnable: false
            },
            rules: {
                ListenPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
                IP: [
                    { required: true, message: '必填', trigger: 'blur' }
                ]
            }
        });

        const loadConfig = () => {
            getConfig().then((res) => {
                state.configInfo = res;
                state.form.TargetName = res.TargetName;
                state.form.IP = res.IP;
                state.form.LanIPs = res.LanIPs.join(',');
                state.form.ListenPort = res.ListenPort;
                state.form.BufferSize = res.BufferSize;
                state.form.ConnectEnable = res.ConnectEnable;
            });
        }

        onMounted(() => {
            loadConfig();
        });
        const formDom = ref(null);
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    state.configInfo.TargetName = state.form.TargetName;
                    state.configInfo.IP = state.form.IP;
                    state.configInfo.LanIPs = state.form.LanIPs.split(',').filter(c => c.length > 0);
                    state.configInfo.ListenPort = +state.form.ListenPort;
                    state.configInfo.BufferSize = +state.form.BufferSize;
                    state.configInfo.ConnectEnable = state.form.ConnectEnable;
                    setConfig(state.configInfo).then(resolve).catch(reject);
                });
            });
        }

        return {
            shareData, targets, state, formDom, submit
        }
    }
}
</script>

<style lang="stylus" scoped>
.el-row {
    width: 100%;
}

.el-form-item:last-child {
    margin-bottom: 0;
}

@media screen and (max-width: 768px) {
    .el-col {
        margin-top: 0.6rem;
    }
}
</style>