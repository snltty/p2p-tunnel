<!--
 * @Author: snltty
 * @Date: 2022-05-14 19:17:29
 * @LastEditors: snltty
 * @LastEditTime: 2022-09-02 22:59:29
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\httpproxy\Index.vue
-->
<template>
    <div class="proxy-wrap">
        <h3 class="title t-c">{{$route.meta.name}}</h3>
        <el-alert class="alert" type="warning" show-icon :closable="false" title="http1.1代理，如果服务端开启，则也可以代理到服务端，在TCP转发配置被访问权限" />
        <div class="form">
            <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
                <el-form-item label="" label-width="0">
                    <div class="w-100">
                        <el-row :gutter="10">
                            <el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
                                <el-form-item label="监听端口" prop="Port">
                                    <el-input v-model="state.form.Port"></el-input>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
                                <el-form-item label="通信通道" prop="TunnelType">
                                    <el-select v-model="state.form.TunnelType" placeholder="选择类型">
                                        <el-option v-for="(item,index) in shareData.tunnelTypes" :key="index" :label="item" :value="index">
                                        </el-option>
                                    </el-select>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="24" :sm="6" :md="6" :lg="6" :xl="6">
                                <el-form-item label="目标端" prop="Name">
                                    <el-select v-model="state.form.Name" placeholder="选择目标">
                                        <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.Name">
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
                            <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                <el-form-item label-width="0" prop="Listening">
                                    <el-checkbox v-model="state.form.Listening" label="开启端口监听" />
                                </el-form-item>
                            </el-col>
                            <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                <el-form-item label-width="0" prop="IsPac">
                                    <el-tooltip class="box-item" effect="dark" content="勾选则设置系统代理，不勾选则需要自己设置" placement="top-start">
                                        <el-checkbox v-model="state.form.IsPac" label="设置系统代理" />
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                            <el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
                                <el-form-item label-width="0" prop="IsCustomPac">
                                    <el-tooltip class="box-item" effect="dark" content="自定义pac还是使用预制的pac规则" placement="top-start">
                                        <el-checkbox v-model="state.form.IsCustomPac" label="自定义pac" />
                                    </el-tooltip>
                                </el-form-item>
                            </el-col>
                        </el-row>
                    </div>
                </el-form-item>
                <el-form-item label-width="0">
                    <div class="w-100 t-c">
                        <el-button type="primary" :loading="state.loading" @click="handleSubmit" class="m-r-1">确 定</el-button>
                        <ConfigureModal className="TcpForwardClientConfigure">
                            <el-button>配置插件</el-button>
                        </ConfigureModal>
                    </div>
                </el-form-item>
                <el-form-item label-width="0" class="hidden-xs-only">
                    <div class="w-100">
                        <el-input v-model="state.form.Pac" :rows="22" type="textarea" placeholder="写pac内容" resize="none" />
                    </div>
                </el-form-item>
            </el-form>
        </div>
    </div>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity'
import { getListProxy, getPac, addListen } from '../../../apis/tcp-forward'
import { onMounted } from '@vue/runtime-core'
import { ElMessage } from 'element-plus'
import { injectClients } from '../../../states/clients'
import { injectShareData } from '../../../states/shareData'
import ConfigureModal from '../configure/ConfigureModal.vue'
export default {
    components: { ConfigureModal },
    setup () {

        const clientsState = injectClients();
        const shareData = injectShareData();
        const loadConfig = () => {
            getListProxy().then((res) => {
                const json = res[0] || {
                    ID: 0,
                    Port: 5412,
                    ForwardType: 2,
                    TunnelType: '8',
                    AliveType: 2,
                    Name: '',
                    Listening: false,
                    Pac: '',
                    IsPac: false,
                    IsCustomPac: false,
                };
                state.form.ID = json.ID;
                state.form.Port = json.Port;
                //state.form.ForwardType = json.ForwardType;
                state.form.TunnelType = json.TunnelType.toString();
                state.form.AliveType = json.AliveType;
                state.form.Name = json.Name;
                state.form.Listening = json.Listening;
                state.form.Pac = json.Pac;
                state.form.IsPac = json.IsPac;
                state.form.IsCustomPac = json.IsCustomPac;

                loadPac();
            });
        }
        const loadPac = () => {
            getPac().then((res) => {
                state.form.Pac = res;
            })
        }
        onMounted(() => {
            loadConfig();
        })

        const targets = computed(() => {
            return [{ Name: '', label: '服务器' }].concat(clientsState.clients.map(c => {
                return { Name: c.Name, label: c.Name }
            }));
        })
        const state = reactive({
            loading: false,
            pac: '',
            form: {
                ID: 0,
                Port: 5413,
                ForwardType: 2,
                TunnelType: '8',
                AliveType: 2,
                Name: '',
                Listening: false,
                Pac: '',
                IsPac: false,
                IsCustomPac: false,
            },
            rules: {
                Port: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform (value) {
                            return Number(value)
                        }
                    }
                ]
            }
        });
        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                const json = JSON.parse(JSON.stringify(state.form));
                json.Port = Number(json.Port);
                json.TunnelType = Number(json.TunnelType);
                addListen(json).then(() => {
                    state.loading = false;
                    ElMessage.success('操作成功！');
                    loadConfig();
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }

        return {
            targets, shareData, state, formDom, handleSubmit
        }
    }
}
</script>

<style lang="stylus" scoped>
.proxy-wrap
    padding: 2rem;

.alert
    margin-bottom: 1rem;

.form
    border: 1px solid #eee;
    padding: 2rem;
    border-radius: 0.4rem;

@media screen and (max-width: 768px)
    .el-col
        margin-top: 0.6rem;
</style>