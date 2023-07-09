<template>
    <div class="form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="state.form" :rules="state.rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="允许连接" prop="ConnectEnable">
                                <el-checkbox v-model="state.form.ConnectEnable">开启
                                    <el-tooltip class="box-item" effect="dark" content="是否允许被连接，不允许则只有权限时可连接" placement="top">
                                        <el-icon>
                                            <Warning />
                                        </el-icon>
                                    </el-tooltip>
                                </el-checkbox>
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
                </el-form-item>
                <el-form-item label-width="0">
                    <div class="t-c w-100">
                        <el-button type="primary" :loading="state.loading" @click="submit">确 定</el-button>
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { ref, reactive } from "@vue/reactivity";
import { getConfig, updateConfig } from "../../../../apis/forward";
import { shareData } from "../../../../states/shareData";
import { onMounted } from "@vue/runtime-core";
import plugin from './plugin'
import { ElMessage } from 'element-plus';
export default {
    plugin: plugin,
    setup() {
        const formDom = ref(null);
        const state = reactive({
            loading: false,
            configInfo: {},
            form: {
                ConnectEnable: false,
                BufferSize: 3
            },
            rules: {},
        });

        const loadConfig = () => {
            getConfig()
                .then((json) => {
                    state.configInfo = json;
                    state.form.ConnectEnable = json.ConnectEnable;
                    state.form.BufferSize = json.BufferSize;
                })
                .catch((msg) => { });
        };
        const getJson = () => {
            let _json = JSON.parse(JSON.stringify(state.configInfo));
            _json.ConnectEnable = state.form.ConnectEnable;
            _json.BufferSize = +state.form.BufferSize;
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
                    state.loading = true;
                    updateConfig(_json)
                        .then((res) => {
                            state.loading = false;
                            resolve();
                            if (res) {
                                ElMessage.success('操作成功!');
                            } else {
                                ElMessage.error('操作失败!');
                            }
                        }).catch(() => {
                            state.loading = false;
                            resolve();
                        })
                });
            });
        };

        onMounted(() => {
            loadConfig();
        });

        return {
            shareData,
            state,
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

.inner {
    padding: 2rem;
}

@media screen and (max-width: 768px) {
    .el-col {
        margin-top: 0.6rem;
    }
}
</style>