<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="state.form" :rules="state.rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="开启" prop="ConnectEnable">
                                <el-checkbox size="default" v-model="state.form.ConnectEnable">开启
                                    <el-tooltip class="box-item" effect="dark" content="允许所有账号使用socks5代理，包括匿名，不允许则只有权限时可使用" placement="top">
                                        <el-icon>
                                            <Warning />
                                        </el-icon>
                                    </el-tooltip>
                                </el-checkbox>
                            </el-form-item>
                        </el-col>
                    </el-row>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { ref, reactive } from '@vue/reactivity';
import { getConfigure, saveConfigure } from '../../../../apis/configure'
import { onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    setup() {
        const formDom = ref(null);
        const state = reactive({
            form: {
                ConnectEnable: true
            },
            rules: {}
        });

        const loadConfig = () => {
            return getConfigure(plugin.config);
        }
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    loadConfig().then((json) => {
                        json.ConnectEnable = state.form.ConnectEnable;
                        saveConfigure(plugin.config, JSON.stringify(json)).then(resolve).catch(reject);
                    }).catch(reject);
                });
            })
        }

        onMounted(() => {
            loadConfig().then((json) => {
                state.form.ConnectEnable = json.ConnectEnable;
            });
        });

        return {
            state, formDom, submit
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