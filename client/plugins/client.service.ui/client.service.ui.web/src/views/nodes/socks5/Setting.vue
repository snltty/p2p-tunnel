<template>
    <div class="socks5-wrap">
        <div class="form">
            <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="监听端口" prop="ListenPort">
                                    <el-input size="default" v-model="state.form.ListenPort"></el-input>
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
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="允许访问" prop="ConnectEnable">
                                    <el-tooltip class="box-item" effect="dark" content="作为目标端时，是否允许被访问" placement="top-start">
                                        <el-checkbox v-model="state.form.ConnectEnable" label="开启" />
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="目标端" prop="TargetName">
                                    <el-select size="default" v-model="state.form.TargetName" placeholder="选择目标">
                                        <el-option v-for="(item, index) in targets" :key="index" :label="item.label" :value="item.Name">
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
                                    <el-tooltip class="box-item" effect="dark" content="勾选则设置系统代理，不勾选则需要自己设置" placement="top-start">
                                        <el-checkbox v-model="state.form.IsPac" label="开启" />
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="自定义pac" prop="IsCustomPac">
                                    <el-tooltip class="box-item" effect="dark" content="自定义pac还是使用预制的pac规则" placement="top-start">
                                        <el-checkbox v-model="state.form.IsCustomPac" label="开启" />
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </div>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <div class="w-100 t-c">
                        <el-button @click="state.showPac = true" size="small">编辑pac</el-button>
                    </div>
                </el-form-item>
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
import { computed, reactive, ref } from "@vue/reactivity";
import { get, set, getPac, updatePac } from "../../../apis/socks5";
import { onMounted } from "@vue/runtime-core";
import { injectClients } from "../../../states/clients";
import { injectShareData } from "../../../states/shareData";
import { ElMessage } from 'element-plus/lib/components'
import plugin from './plugin'
export default {
    plugin: plugin,
    components: {},
    setup() {
        const clientsState = injectClients();
        const shareData = injectShareData();
        const targets = computed(() => {
            return [{ Name: "/", label: "服务器" }].concat(
                clientsState.clients.map((c) => {
                    return { Name: c.Name, label: c.Name };
                })
            );
        });
        const state = reactive({
            configInfo: {},
            showPac: false,
            pac: "",
            form: {
                ListenPort: 5413,
                ConnectEnable: false,
                BufferSize: 3,
                TargetName: "",
                IsPac: false,
                IsCustomPac: false,
            },
            rules: {
                ListenPort: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 1024,
                        max: 65535,
                        message: "数字 1024-65535",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ]
            },
        });

        const loadConfig = () => {
            get().then((res) => {
                state.configInfo = res;
                state.form.ListenPort = res.ListenPort;
                state.form.BufferSize = res.BufferSize;
                state.form.ConnectEnable = res.ConnectEnable;
                state.form.TargetName = res.TargetName;
                state.form.IsPac = res.IsPac;
                state.form.IsCustomPac = res.IsCustomPac;
            });
        };
        const handlePacCancel = () => {
            state.showPac = false;
        };
        const handlePac = () => {
            state.showPac = false;
            updatePac(state.pac);
            ElMessage.success('已更新');
        };

        const loadPac = () => {
            getPac().then((res) => {
                state.pac = res;
            });
        };
        onMounted(() => {
            loadConfig();
            loadPac();
        });

        const formDom = ref(null);
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    state.configInfo.ListenPort = +state.form.ListenPort;
                    state.configInfo.BufferSize = +state.form.BufferSize;
                    state.configInfo.ConnectEnable = state.form.ConnectEnable;
                    state.configInfo.TargetName = state.form.TargetName;
                    state.configInfo.IsPac = state.form.IsPac;
                    state.configInfo.IsCustomPac = state.form.IsCustomPac;
                    set(state.configInfo).then(resolve).catch(reject);

                });
            });
        };

        return {
            targets,
            shareData,
            state,
            formDom,
            submit,
            handlePacCancel,
            handlePac,
        };
    },
};
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