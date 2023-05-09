<template>
    <a href="javascript:;" class="modes-wrap" @click="handleShow">功能开关</a>
    <el-dialog v-model="state.show" title="显示或隐藏一些功能" width="400" center top="1vh">
        <div>
            <h3>节点功能</h3>
            <el-checkbox-group v-model="state.checkListNodes">
                <template v-for="(item,index) in nodesPlugins" :key="index">
                    <el-checkbox :label="item.service" :checked="access(item.service)">{{item.text}}</el-checkbox>
                </template>
            </el-checkbox-group>
            <h3>服务器功能</h3>
            <el-checkbox-group v-model="state.checkListServer">
                <template v-for="(item,index) in serverPlugins" :key="index">
                    <el-checkbox :label="item.service" :checked="access(item.service)">{{item.text}}</el-checkbox>
                </template>
            </el-checkbox-group>
        </div>
        <template #footer>
            <span class="dialog-footer">
                <el-button :loading="state.loading" @click="handleCancel">取消</el-button>
                <el-button type="primary" :loading="state.loading" @click="handleConfirm">应用</el-button>
            </span>
        </template>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity'
import { getSignInInfo, updateConfig } from '../../apis/signin'
import { injectServices, accessService } from '../../states/services'
import { ElMessage } from 'element-plus/lib/components';
export default {
    setup() {

        const nodesFiles = require.context('../../views/nodes/', true, /plugin\.js/);
        const nodesPlugins = nodesFiles.keys().map(c => nodesFiles(c).default);

        const serverFiles = require.context('../../views/server/', true, /plugin\.js/);
        const serverPlugins = serverFiles.keys().map(c => serverFiles(c).default);

        const servicesState = injectServices();
        const state = reactive({
            show: false,
            checkListNodes: [],
            checkListServer: [],
            loading: false
        });

        const access = (service) => {
            return accessService(service, servicesState);
        }
        const handleCancel = () => {
            state.show = false;
        }
        const handleShow = () => {
            state.show = true;
        }
        const handleConfirm = () => {
            state.loading = true;
            getSignInInfo().then((json) => {
                json.ClientConfig.Services = state.checkListNodes.concat(state.checkListServer);
                state.loading = false;
                updateConfig(json).then(() => {
                    ElMessage.success('成功，刷新生效');
                    servicesState.update();
                }).catch((e) => {
                    ElMessage.error('失败' + e);
                });
            }).catch((e) => {
                ElMessage.error('失败' + e);
                state.loading = false;
            });
        }

        return {
            state, nodesPlugins, serverPlugins, handleCancel, handleShow, handleConfirm, access
        }
    }
}
</script>

<style lang="stylus" scoped>
.modes-wrap {
    padding-top: 0.6rem;
    line-height: normal;
    color: #666;
    text-decoration: underline;
}
</style>