<!--
 * @Author: snltty
 * @Date: 2021-09-24 14:36:58
 * @LastEditors: snltty
 * @LastEditTime: 2022-12-05 11:06:23
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\service\configure\ConfigureModal.vue
-->
<template>
    <span @click="handleEdit">
        <slot>
            <el-button size="small">配置</el-button>
        </slot>
    </span>
    <el-dialog title="配置" v-model="showAdd" center :close-on-click-modal="false" append-to-body width="80rem">
        <el-form ref="formDom" :model="form" :rules="rules" label-width="0">
            <el-form-item label="" prop="Content" label-width="0">
                <el-input type="textarea" v-model="form.Content" :autosize="{minRows:10,maxRows:30}" />
            </el-form-item>
            <el-form-item label="" label-width="0">
                <div class="t-c w-100">无内容，或配置失败，为未注册或未拥有配置权限</div>
            </el-form-item>
        </el-form>
        <template #footer>
            <el-button @click="showAdd = false">取 消</el-button>
            <el-button type="primary" :loading="loading" @click="handleSubmit">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { toRefs, reactive, ref } from '@vue/reactivity';
import { getConfigure, saveConfigure } from '../../../apis/configure'
import { ElMessage } from 'element-plus'
export default {
    props: ['className'],
    emits: ['success'],
    setup (props, { emit }) {
        const state = reactive({
            loading: false,
            showAdd: false,
            showEditor: false,
            form: {
                ClassName: props.className,
                Content: ''
            },
            rules: {
            }
        });
        const handleEdit = () => {
            state.showAdd = true;
            state.showEditor = false;
            getConfigure(state.form.ClassName).then((res) => {
                state.form.Content = res;
            });
        }

        const formDom = ref(null);
        const handleSubmit = () => {
            formDom.value.validate((valid) => {
                if (!valid) {
                    return false;
                }
                state.loading = true;
                //  saveConfigure(state.form.ClassName, JSON.stringify(editor.get())).then((res) => {
                saveConfigure(state.form.ClassName, state.form.Content).then((res) => {
                    if (res) {
                        ElMessage.error(res);
                    } else {
                        state.loading = false;
                        state.showAdd = false;
                        ElMessage.success('已保存');
                        emit('success');
                    }
                }).catch((e) => {
                    ElMessage.error(e);
                    state.loading = false;
                });
            })
        }

        return {
            ...toRefs(state), formDom, handleEdit, handleSubmit
        }
    }
}
</script>
<style lang="stylus" scoped>
#editor
    width: 100%;
</style>
<style lang="stylus">
.jsoneditor-outer
    height: 30rem;
    margin: 0;
    padding: 0;
    border: 1px solid #ddd;

div.jsoneditor-menu, .jsoneditor-statusbar
    display: none;
</style>