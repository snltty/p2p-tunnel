<!--
 * @Author: snltty
 * @Date: 2022-05-14 19:17:29
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 21:07:09
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\setting\HttpProxy.vue
-->
<template>
    <div class="proxy-wrap">
        <div class="form">
            <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="监听端口" prop="Port">
                                    <el-input size="default" v-model="state.form.Port"></el-input>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                                <el-form-item label="目标端" prop="Name">
                                    <el-select size="default" v-model="state.form.Name" placeholder="选择目标">
                                        <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
                                        </el-option>
                                    </el-select>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </div>
                </el-form-item>
                <el-form-item label="" label-width="0">
                    <div class="w-100 t-c">
                        <el-button @click="state.showPac = true;" size="small">编辑pac</el-button>
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
import { computed, reactive, ref } from '@vue/reactivity'
import { getListProxy, getPac, addListen } from '../../apis/tcp-forward'
import { onMounted } from '@vue/runtime-core'
import { injectClients } from '../../states/clients'
import { injectShareData } from '../../states/shareData'
export default {
    components: {},
    setup () {

        const clientsState = injectClients();
        const shareData = injectShareData();
        const targets = computed(() => {
            return [{ Name: '', label: '服务器' }].concat(clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            }));
        })
        const state = reactive({
            configInfo: {},
            showPac: false,
            pac: '',
            form: {
                Port: 5413,
                Name: '',
                Pac: '',
            },
            rules: {
                Port: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1024, max: 65535, message: '数字 1024-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });
        const loadConfig = () => {
            getListProxy().then((res) => {
                const json = res || {
                    ID: 0,
                    Port: 5412,
                    ForwardType: shareData.forwardTypes.proxy,
                    AliveType: shareData.aliveTypesName.web,
                    Name: '',
                    Listening: false,
                    Pac: '',
                    IsPac: false,
                    IsCustomPac: false,
                };
                state.configInfo = json;
                state.form.Port = json.Port;
                state.form.Name = json.Name;
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
        }
        const formDom = ref(null);
        const submit = () => {
            return new Promise((resolve, reject) => {
                formDom.value.validate((valid) => {
                    if (valid == false) {
                        reject();
                        return false;
                    }
                    state.configInfo.Name = state.form.Name;
                    state.configInfo.Port = +state.form.Port;
                    state.configInfo.Pac = state.form.Pac;
                    addListen(state.configInfo).then(resolve).catch(reject);
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