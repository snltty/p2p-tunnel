<!--
 * @Author: snltty
 * @Date: 2022-05-14 19:17:29
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 20:34:37
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\setting\Vea.vue
-->
<template>
    <div class="socks5-wrap">
        <div class="inner">
            <div class="form">
                <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                    <el-form-item label="" label-width="0">
                        <div class="w-100">
                            <el-row :gutter="10">
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="代理端口" prop="SocksPort">
                                        <el-tooltip class="box-item" effect="dark" content="代理端口，无所谓，填写一个未被占用的端口即可" placement="top-start">
                                            <el-input size="default" v-model="state.form.SocksPort"></el-input>
                                        </el-tooltip>

                                    </el-form-item>
                                </el-col>
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="buffersize" prop="BufferSize">
                                        <el-input size="default" v-model="state.form.BufferSize"></el-input>
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
                                <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                    <el-form-item label="目标端" prop="TargetName">
                                        <el-tooltip class="box-item" effect="dark" content="当遇到不存在的ip时，目标端应该选择谁，为某个客户端" placement="top-start">
                                            <el-select size="default" v-model="state.form.TargetName" placeholder="选择目标">
                                                <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
                                                </el-option>
                                            </el-select>
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
import { getConfig, setConfig } from '../../apis/vea'
import { onMounted } from '@vue/runtime-core'
import { injectClients } from '../../states/clients'
export default {
    components: {},
    setup () {

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
                SocksPort: 5415,
                BufferSize: 8 * 1024,
                ConnectEnable: false
            },
            rules: {
                BufferSize: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1024, max: 65536, message: '数字 1k-64k', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                SocksPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ],
                IP: [
                    { required: true, message: '必填', trigger: 'blur' }
                ]
            }
        });
        const formDom = ref(null);

        const loadConfig = () => {
            getConfig().then((res) => {
                state.configInfo = res;
                state.form.TargetName = res.TargetName;
                state.form.IP = res.IP;
                state.form.LanIPs = res.LanIPs.join(',');
                state.form.SocksPort = res.SocksPort;
                state.form.BufferSize = res.BufferSize;
                state.form.ConnectEnable = res.ConnectEnable;
            });
        }

        onMounted(() => {
            loadConfig();
        });

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
                    state.configInfo.SocksPort = +state.form.SocksPort;
                    state.configInfo.BufferSize = +state.form.BufferSize;
                    state.configInfo.ConnectEnable = state.form.ConnectEnable;
                    setConfig(state.configInfo).then(resolve).catch(reject);
                });
            });
        }

        return {
            targets, state, formDom, submit
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