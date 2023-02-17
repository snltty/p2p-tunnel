<template>
    <div class="form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="开启" prop="Enable">
                                <el-tooltip class="box-item" effect="dark" content="开启" placement="top-start">
                                    <el-checkbox v-model="model.enable">开启</el-checkbox>
                                </el-tooltip>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="显示数量" prop="MaxLength">
                                <el-tooltip class="box-item" effect="dark" content="日志显示数量" placement="top-start">
                                    <el-input size="default" v-model="model.maxLength"></el-input>
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
import { getConfig, updateConfig } from "../../../apis/logger";
import { onMounted } from "@vue/runtime-core";
export default {
    service: 'LoggerClientService',
    setup() {
        const formDom = ref(null);
        const state = reactive({
            configInfo: {},
            model: {
                enable: false,
                maxLength: 0,
            },
            rules: {
                maxLength: [
                    { required: true, message: "必填", trigger: "blur" },
                    {
                        type: "number",
                        min: 1,
                        max: 10000,
                        message: "数字 1-10000",
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
                    state.model.enable = json.Enable;
                    state.model.maxLength = json.MaxLength;
                })
                .catch((msg) => { });
        };
        const getJson = () => {
            let _json = JSON.parse(JSON.stringify(state.configInfo));
            _json.Enable = state.model.enable;
            _json.MaxLength = +state.model.maxLength;
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