<template>
    <div class="proxy-wrap">
        <div class="form">
            <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="监听端口" prop="ListenPort">
                                    <el-input size="default" v-model="state.form.ListenPort" placeholder="监听端口，随便一个空闲的端口即可">
                                        <template #append>
                                            <el-tooltip class="box-item" effect="dark" content="监听端口，随便一个空闲的端口即可" placement="top">
                                                <el-icon>
                                                    <Warning />
                                                </el-icon>
                                            </el-tooltip>
                                        </template>
                                    </el-input>
                                </el-form-item>
                            </el-col>
                            <high-config>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="bufsize" prop="BufferSize">
                                        <el-select size="default" v-model="state.form.BufferSize" placeholder="选择合适的buff">
                                            <el-option v-for="(item,index) in shareData.bufferSizes" :key="index" :label="item" :value="index"></el-option>
                                        </el-select>
                                    </el-form-item>
                                </el-col>
                            </high-config>
                        </el-row>
                    </div>
                </el-form-item>
                <high-config>
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="绑定ip" prop="ProxyIp">
                                        <el-input size="default" v-model="state.form.ProxyIp" placeholder="作为代理目标ip">
                                            <template #append>
                                                <el-tooltip class="box-item" effect="dark" content="作为代理目标ip" placement="top">
                                                    <el-icon>
                                                        <Warning />
                                                    </el-icon>
                                                </el-tooltip>
                                            </template>
                                        </el-input>
                                    </el-form-item>
                                </el-col>
                            </el-row>
                        </div>
                    </el-form-item>
                </high-config>
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="允许访问" prop="ConnectEnable">
                                    <el-checkbox v-model="state.form.ConnectEnable">开启
                                        <el-tooltip class="box-item" effect="dark" content="作为目标端时，是否允许被访问，不允许则只有权限时可访问" placement="top">
                                            <el-icon>
                                                <Warning />
                                            </el-icon>
                                        </el-tooltip>
                                    </el-checkbox>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="目标端" prop="Name">
                                    <el-select size="default" v-model="state.form.TargetConnectionId" placeholder="选择目标">
                                        <el-option v-for="(item, index) in targets" :key="index" :label="item.label" :value="item.id">
                                        </el-option>
                                    </el-select>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </div>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="系统代理" prop="IsPac">
                                    <el-checkbox v-model="state.form.IsPac">开启
                                        <el-tooltip class="box-item" effect="dark" content="勾选则设置系统代理，不勾选则需要自己设置" placement="top">
                                            <el-icon>
                                                <Warning />
                                            </el-icon>
                                        </el-tooltip>
                                    </el-checkbox>
                                </el-form-item>
                            </el-col>
                            <high-config>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="自定义pac" prop="IsCustomPac">
                                        <el-checkbox v-model="state.form.IsCustomPac">开启
                                            <el-tooltip class="box-item" effect="dark" content="自定义pac还是使用预制的pac规则" placement="top">
                                                <el-icon>
                                                    <Warning />
                                                </el-icon>
                                            </el-tooltip>
                                        </el-checkbox>
                                    </el-form-item>
                                </el-col>
                            </high-config>
                        </el-row>
                    </div>
                </el-form-item>
                <high-config>
                    <el-form-item label="" label-width="0">
                        <div class="w-100 t-c">
                            <el-button @click="state.showPac = true;" size="small">编辑pac</el-button>
                        </div>
                    </el-form-item>
                </high-config>
            </el-form>
        </div>
    </div>
    <el-dialog title="编辑自定义pac规则" append-to-body destroy-on-close v-model="state.showPac" center :close-on-click-modal="false" width="60rem" top="1vh">
        <el-input v-model="state.pac" :rows="18" type="textarea" placeholder="写pac内容" resize="none" />
        <template #footer>
            <el-button @click="handlePacCancel">取 消</el-button>
            <el-button type="primary" @click="handlePac">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity'
import { getPac, setPac } from '../../../../apis/httpproxy'
import { getConfigure, saveConfigure } from '../../../../apis/configure'
import { onMounted } from '@vue/runtime-core'
import { injectClients } from '../../../../states/clients'
import { injectShareData } from '../../../../states/shareData'
import plugin from './plugin'
import { ElMessage } from 'element-plus/lib/components'
export default {
    plugin: plugin,
    components: {},
    setup() {

        const clientsState = injectClients();
        const shareData = injectShareData();
        const targets = computed(() => {
            return [{ id: 0, label: '服务器' }].concat(clientsState.clients.map(c => {
                return { id: c.ConnectionId, label: c.Name }
            }));
        })
        const state = reactive({
            showPac: false,
            pac: '',
            form: {
                ListenPort: 5414,
                IsPac: false,
                IsCustomPac: false,
                BufferSize: 3,
                ConnectEnable: false,
                TargetConnectionId: 0,
                Pac: '',
                ProxyIp: '127.0.0.1'
            },
            rules: {
                ListenPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1024, max: 65535, message: '数字 1024-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });
        const loadConfig = () => {
            getConfigure(plugin.config).then((json) => {
                state.form.ListenPort = json.ListenPort;
                state.form.TargetConnectionId = json.TargetConnectionId;
                state.form.IsPac = json.IsPac;
                state.form.IsCustomPac = json.IsCustomPac;
                state.form.BufferSize = json.BufferSize;
                state.form.ConnectEnable = json.ConnectEnable;
                state.form.ProxyIp = json.ProxyIp;
                loadPac();
            });
        }
        const loadPac = () => {
            getPac().then((res) => {
                state.form.Pac = res;
                state.pac = res;
            })
        }
        onMounted(() => {
            loadConfig();
        })

        const handlePacCancel = () => {
            state.showPac = false;
        }
        const handlePac = () => {
            state.form.Pac = state.pac;
            state.showPac = false;
            setPac(state.form.Pac);
            ElMessage.success('已更新');
        }
        const formDom = ref(null);
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    getConfigure(plugin.config).then((json) => {
                        json.TargetConnectionId = state.form.TargetConnectionId;
                        json.ListenPort = +state.form.ListenPort;
                        json.IsPac = state.form.IsPac;
                        json.IsCustomPac = state.form.IsCustomPac;
                        json.BufferSize = +state.form.BufferSize;
                        json.ConnectEnable = state.form.ConnectEnable;
                        json.ProxyIp = state.form.ProxyIp;
                        saveConfigure(plugin.config, JSON.stringify(json)).then(resolve).catch(reject);
                    });

                });
            });
        }

        return {
            targets, shareData, state, formDom, submit, handlePacCancel, handlePac
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