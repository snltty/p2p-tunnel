<template>
    <a href="javascript:;" class="guide-wrap" @click="handleShow">新手引导?</a>
    <el-dialog v-model="state.show" title="新手引导项" :modal="false" center draggable append-to-body width="300" top="1vh">
        <div>

        </div>
        <template #footer>
            <span class="dialog-footer">
                <el-button @click="handleCancel">取消</el-button>
                <el-button type="primary" @click="handleCancel">确定</el-button>
            </span>
        </template>
    </el-dialog>
</template>

<script>
import { reactive } from '@vue/reactivity'
import Driver from "driver.js";
export default {
    setup() {
        const state = reactive({
            show: false
        });
        const handleCancel = () => {
            state.show = false;
        }
        const handleShow = () => {
            // state.show = true;
            const driver = new Driver({ allowClose: false });
            document.getElementById('server-select').click();
            setTimeout(() => {
                driver.defineSteps([
                    {
                        element: '#server-select',
                        popover: {
                            title: '1、点击选择服务器节点',
                            description: '点击选择服务器节点',
                        }
                    },
                    {
                        element: '#add-server',
                        popover: {
                            title: '2、添加服务器节点',
                            description: '添加一个服务器节点',
                        }
                    },
                    {
                        element: '#select-server',
                        popover: {
                            title: '3、选择服务器节点',
                            description: '选择一个可用的服务器节点',
                        }
                    },
                    {
                        element: '#connect-btn',
                        popover: {
                            title: '4、点击按钮，连接服务器',
                            description: '',
                        }
                    }

                ]);
                driver.start();
            }, 1000)
        }
        return {
            state, handleCancel, handleShow
        }
    }
}
</script>

<style lang="stylus" scoped>
.guide-wrap {
    padding-top: 0.6rem;
    line-height: normal;
    color: #666;
    text-decoration: underline;
}
</style>