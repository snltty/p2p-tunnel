<template>
    <el-dialog title="转发" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="500px">
        <el-form ref="formDom" :model="state.form" :rules="state.rules" label-width="80px">
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="源host" prop="SourceIp">
                            <el-input :disabled="addForwardData.currentLsiten.AliveType == shareData.aliveTypesName.tunnel" v-model="state.form.SourceIp"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="目标端" prop="ConnectionId">
                            <el-select v-model="state.form.ConnectionId" placeholder="选择目标">
                                <el-option v-for="(item,index) in targets" :key="index" :label="item.label" :value="item.id">
                                </el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="" label-width="0">
                <el-row>
                    <el-col :span="12">
                        <el-form-item label="目标ip" prop="TargetIp">
                            <el-input v-model="state.form.TargetIp"></el-input>
                        </el-form-item>
                    </el-col>
                    <el-col :span="12">
                        <el-form-item label="目标端口" prop="TargetPort">
                            <el-input v-model="state.form.TargetPort"></el-input>
                        </el-form-item>
                    </el-col>
                </el-row>
            </el-form-item>
            <el-form-item label="简单说明" prop="Desc">
                <el-input v-model="state.form.Desc"></el-input>
            </el-form-item>
        </el-form>
        <div class="remark t-c" v-html="remark"></div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" :loading="state.loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { computed, reactive, ref } from '@vue/reactivity';
import { inject, watch } from '@vue/runtime-core';
import { addForward } from '../../../apis/forward'
import { injectClients } from '../../../states/clients'
import { injectShareData } from '../../../states/shareData'
export default {
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {

        const shareData = injectShareData();
        const defaultForm = {
            ListenID: 0,
            ID: 0,
            SourceIp: '0.0.0.0',
            id: 0, TargetIp: '127.0.0.1', TargetPort: 80,
            AliveType: shareData.aliveTypesName.tunnel + '',
            Desc: '',
            ConnectionId: 0
        };
        const clientsState = injectClients();
        const targets = computed(() => {
            return [{ id: 0, label: '服务器' }].concat(clientsState.clients.map(c => {
                return { id: c.ConnectionId, label: `${c.Name}` }
            }));
        });
        const targetJson = computed(() => {
            return targets.value.reduce((value, item) => {
                value[item.id] = item.label;
                return value;
            }, {})
        });

        const addForwardData = inject('add-forward-data');

        const state = reactive({
            show: props.modelValue,
            loading: false,
            form: {
                ID: addForwardData.value.forward.ID || defaultForm.ID,
                SourceIp: addForwardData.value.forward.SourceIp || defaultForm.SourceIp,
                ConnectionId: addForwardData.value.forward.ConnectionId || defaultForm.ConnectionId,
                TargetIp: addForwardData.value.forward.TargetIp || defaultForm.TargetIp,
                TargetPort: addForwardData.value.forward.TargetPort || defaultForm.TargetPort,
                Desc: addForwardData.value.forward.Desc || defaultForm.Desc
            },
            rules: {
                SourceIp: [{ required: true, message: '必填', trigger: 'blur' }],
                TargetIp: [{ required: true, message: '必填', trigger: 'blur' }],
                TargetPort: [
                    { required: true, message: '必填', trigger: 'blur' },
                    {
                        type: 'number', min: 1, max: 65535, message: '数字 1-65535', trigger: 'blur', transform(value) {
                            return Number(value)
                        }
                    }
                ],
            }
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const remark = computed(() => {
            return [
                `本节点(${state.form.SourceIp}:${addForwardData.value.currentLsiten.Port})`,
                `<br/>`,
                ` -> `,
                `【${shareData.aliveTypes[addForwardData.value.currentLsiten.AliveType]}】`,
                ` -> `,
                `<br/>`,
                `${targetJson.value[state.form.ConnectionId]}(${state.form.TargetIp}:${state.form.TargetPort})`
            ].join('');
        });

        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;

                state.form.TargetPort = Number(state.form.TargetPort);
                const json = {
                    ListenID: addForwardData.value.currentLsiten.ID,
                    Forward: state.form
                }
                addForward(json).then(() => {
                    state.loading = false;
                    state.show = false;
                    emit('success');
                }).catch((e) => {
                    state.loading = false;
                });
            })
        }
        const handleCancel = () => {
            state.show = false;
        }

        return {
            shareData, state, targets, addForwardData, formDom, remark, handleSubmit, handleCancel
        }
    }
}
</script>
<style lang="stylus" scoped>
.remark {
    margin-top: 1rem;
}
</style>