<template>
    <div class="vea-dhcp-setting-wrap flex flex-column h-100">
        <el-tabs type="border-card">
            <el-tab-pane label="配置">
                <Setting></Setting>
            </el-tab-pane>
        </el-tabs>
    </div>
</template>

<script>
import { reactive } from 'vue';
import plugin from './plugin'
import Setting from './Setting1.vue'
export default {
    plugin: plugin,
    components: { Setting },
    setup() {

        const state = reactive({});
        return { state }
    }
}
</script>

<style lang="stylus" scoped>
.vea-dhcp-setting-wrap {
    padding: 2rem;
}
</style>