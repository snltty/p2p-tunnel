<template>
    <div class="register-form">
        <div class="inner">
            <el-form label-width="8rem" ref="formDom" :model="model" :rules="rules">
                <el-form-item label="" label-width="0">
                    <el-row>
                        <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                            <el-form-item label="开启" prop="ConnectEnable">
                                <el-tooltip class="box-item" effect="dark" content="开启socks5代理" placement="top-start">
                                    <el-checkbox size="default" v-model="model.ConnectEnable">开启</el-checkbox>
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
import { ref, toRefs, reactive } from '@vue/reactivity';
import { getConfigure, saveConfigure } from '../../../apis/configure'
import { shareData } from '../../../states/shareData'
import { onMounted } from '@vue/runtime-core';
export default {
    service: 'ServerUdpForwardClientService',
    access: shareData.serverAccess.setting.value,
    setup() {
        const formDom = ref(null);
        const state = reactive({
            model: {
                ConnectEnable: true
            },
            rules: {}
        });

        const loadConfig = () => {
            return new Promise((resolve, reject) => {
                getConfigure('ServerSocks5Configure').then((json) => {
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
                        json.ConnectEnable = state.model.ConnectEnable;
                        saveConfigure('ServerSocks5Configure', JSON.stringify(json)).then(resolve).catch(reject);
                    }).catch(reject);
                });
            })
        }

        onMounted(() => {
            loadConfig().then((json) => {
                state.model.ConnectEnable = json.ConnectEnable;
            });
        });

        return {
            ...toRefs(state), formDom, submit
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