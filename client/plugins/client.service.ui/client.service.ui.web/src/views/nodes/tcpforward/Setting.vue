<template>
    <div class="form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="允许连接" prop="ConnectEnable">
                                <el-tooltip class="box-item" effect="dark" content="是否允许被连接" placement="top-start">
                                    <el-checkbox v-model="model.ConnectEnable">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="bufsize" prop="BufferSize">
                                <el-tooltip class="box-item" effect="dark" content="tcp连接的buffer size" placement="top-start">
                                    <el-input size="default" v-model="model.BufferSize"></el-input>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { ref, toRefs, reactive } from "@vue/reactivity";
import { getConfig, updateConfig } from "../../../apis/tcp-forward";
import { accessServiceOr } from "../../../states/services";
import { onMounted } from "@vue/runtime-core";
export default {
    serviceCallback: (state) => {
        return accessServiceOr(['TcpForwardClientService', 'ServerTcpForwardClientService'], state);
    },
    setup() {
        const formDom = ref(null);
        const state = reactive({
            configInfo: {},
            model: {
                ConnectEnable: false,
                BufferSize: 0,
            },
            rules: {
                BufferSize: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 1024,
                        max: 2147483647,
                        message: "数字 1024-2147483647",
                        trigger: "blur",
                        transform(value) {
                            return Number(value);
                        },
                    },
                ],
            },
        });

        const loadConfig = () => {
            getConfig()
                .then((json) => {
                    json = new Function("return" + json)();
                    state.configInfo = json;
                    state.model.ConnectEnable = json.ConnectEnable;
                    state.model.BufferSize = json.BufferSize;
                })
                .catch((msg) => { });
        };
        const getJson = () => {
            let _json = JSON.parse(JSON.stringify(state.configInfo));
            _json.ConnectEnable = state.model.ConnectEnable;
            _json.BufferSize = +state.model.BufferSize;
            return _json;
        };
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    const _json = getJson();
                    updateConfig(JSON.stringify(_json))
                        .then(resolve)
                        .catch(reject);
                });
            });
        };

        onMounted(() => {
            loadConfig();
        });

        return {
            ...toRefs(state),
            formDom,
            submit,
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