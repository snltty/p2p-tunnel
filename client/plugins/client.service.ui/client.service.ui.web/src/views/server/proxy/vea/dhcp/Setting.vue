<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="state.form" :rules="state.rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="开启" prop="Enable">
                                <el-checkbox size="default" v-model="state.form.Enable">开启
                                    <el-tooltip class="box-item" effect="dark" content="允许所有账号使用自动分配ip功能" placement="top">
                                        <el-icon>
                                            <Warning />
                                        </el-icon>
                                    </el-tooltip>
                                </el-checkbox>
                            </el-form-item>
                        </el-col>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="默认网段" prop="DefaultIP">
                                <el-input size="default" v-model="state.form.DefaultIP" placeholder="默认网段">
                                    <template #append>
                                        <el-tooltip class="box-item" effect="dark" content="默认的ip网段，当然，节点可以自己决定用什么网段" placement="top">
                                            <el-icon>
                                                <Warning />
                                            </el-icon>
                                        </el-tooltip>
                                    </template>
                                </el-input>
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
import { getConfigure, saveConfigure } from '../../../../../apis/configure'
import { onMounted } from '@vue/runtime-core';
import plugin from './plugin'
export default {
    plugin: plugin,
    setup() {
        const formDom = ref(null);
        const state = reactive({
            form: {
                Enable: true,
                DefaultIP: '192.168.54.0',
            },
            rules: {}
        });

        const loadConfig = () => {
            return new Promise((resolve, reject) => {
                getConfigure(plugin.config).then((json) => {
                    resolve(new Function(`return ${json}`)());
                }).catch(reject);
            });
        }
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    loadConfig().then((json) => {
                        json.Enable = state.form.Enable;
                        json.DefaultIP = state.form.DefaultIP;
                        saveConfigure(plugin.config, JSON.stringify(json)).then(resolve).catch(reject);
                    }).catch(reject);
                });
            })
        }

        onMounted(() => {
            loadConfig().then((json) => {
                console.log(json);
                state.form.Enable = json.Enable;
                state.form.DefaultIP = json.DefaultIP;
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