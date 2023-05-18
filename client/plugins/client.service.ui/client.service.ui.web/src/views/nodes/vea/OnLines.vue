<template>
    <el-dialog title="在线设备" top="1vh" destroy-on-close v-model="state.show" center :close-on-click-modal="false" width="500px">
        <div class="vea-online-wrap scrollbar">
            <el-row :gutter="10">
                <template v-for="(item,index) in state.items" :key="index">
                    <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12">
                        <div class="item" :class="{online:item.online}">
                            <p><strong>{{item.ip}}</strong></p>
                            <p>{{item.name}}</p>
                        </div>
                    </el-col>
                </template>
            </el-row>
        </div>
        <template #footer>
            <el-button @click="handleCancel">取 消</el-button>
            <el-button type="primary" @click="handleCancel">确 定</el-button>
        </template>
    </el-dialog>
</template>

<script>
import { onMounted, onUnmounted, reactive, watch } from 'vue';
import { getOnlines, onlines } from '../../../apis/vea'
export default {
    props: ['modelValue', 'id'],
    emits: ['update:modelValue', 'success'],
    setup(props, { emit }) {

        const id = props.id;
        const state = reactive({
            show: props.modelValue,
            items: []
        });
        watch(() => state.show, (val) => {
            if (!val) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        let timer = 0;
        onMounted(() => {
            loadData();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        });
        const loadData = () => {
            getOnlines(id).then(() => {
                onlines(id).then((res) => {
                    let arr = [];
                    for (let ip in res.Items) {
                        let item = res.Items[ip];
                        arr.push({
                            ip: (+ip).toIpv4Str(),
                            online: item.Online,
                            name: item.Name,
                        })
                    }
                    state.items = arr;
                    timer = setTimeout(loadData, 1000);
                }).catch(() => {
                    timer = setTimeout(loadData, 1000);
                });
            }).catch(() => {
                timer = setTimeout(loadData, 1000);
            });

        }

        const handleCancel = () => {
            state.show = false;
        }
        return {
            state, handleCancel
        }
    }
}
</script>

<style lang="stylus" scoped>
.vea-online-wrap {
    max-height: 50rem;
    padding: 0 1rem;
}

.item {
    border: 1px solid #ddd;
    margin-bottom: 1rem;
    border-radius: 0.4rem;
    padding: 1rem;

    &.online {
        strong {
            color: green;
        }
    }
}
</style>