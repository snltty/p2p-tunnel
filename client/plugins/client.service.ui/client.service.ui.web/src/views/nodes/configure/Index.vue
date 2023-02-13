<template>
    <div class="plugin-setting-wrap h-100 flex flex-column">
        <div class="head t-c">
            <el-select v-model="state.name" class="m-2" placeholder="Select" @change="handleChange">
                <el-option v-for="item in state.list" :key="item.ClassName" :label="item.Name" :value="item.ClassName" />
            </el-select>
            <el-button type="primary" :loading="state.loading" @click="handleSubmit" style="margin-left:0.4rem">保存</el-button>
        </div>
        <div class="flex-1">
            <el-input type="textarea" v-model="state.content" />
        </div>
    </div>
</template>
<script>
import { reactive, ref } from '@vue/reactivity'
import { getConfigures, getConfigure, saveConfigure } from '../../../apis/configure'
import { ElMessage } from 'element-plus/lib/components'
export default {
    components: {},
    setup() {
        const editor = ref(null);
        const state = reactive({
            loading: false,
            name: '',
            content: '',
            list: []
        });
        const getData = () => {
            getConfigures().then((res) => {
                state.list = res;
                if (res.length > 0) {
                    state.name = res[0].ClassName;
                    loadConfig();
                }
            });
        };
        getData();

        const loadConfig = () => {
            getConfigure(state.name).then((res) => {
                state.content = res;
            });
        }
        const handleChange = (name) => {
            state.name = name;
            loadConfig();
        }
        const handleSubmit = () => {
            state.loading = true;
            saveConfigure(state.name, state.content).then((res) => {
                state.loading = false;
                ElMessage.success('已保存');
            }).catch((e) => {
                state.loading = false;
            });
        }


        return {
            state, editor, getData, handleChange, handleSubmit
        }
    }
}
</script>
<style lang="stylus">
.plugin-setting-wrap {
    .el-textarea, textarea {
        width: 100%;
        height: 100%;
        resize: none;
    }
}
</style>
<style lang="stylus" scoped>
.plugin-setting-wrap {
    padding: 2rem;
    box-sizing: border-box;
}

.head {
    margin-bottom: 0.6rem;

    .el-button {
        vertical-align: middle;
    }
}
</style>