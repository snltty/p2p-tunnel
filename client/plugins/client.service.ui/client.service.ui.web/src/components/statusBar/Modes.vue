<template>
    <div class="modes-wrap">
        <el-dropdown size="small" title="不同的模式，可能会隐藏部分功能，及自动修改部分配置">
            <span class="el-dropdown-link">
                <span>完全模式</span>
                <el-icon class="el-icon--right">
                    <arrow-down />
                </el-icon>
            </span>
            <template #dropdown>
                <el-dropdown-menu>
                    <el-dropdown-item>完全模式</el-dropdown-item>
                    <el-dropdown-item>打洞穿透模式</el-dropdown-item>
                    <el-dropdown-item>代理穿透模式</el-dropdown-item>
                    <el-dropdown-item>代理模式</el-dropdown-item>
                    <el-dropdown-item>自定义模式</el-dropdown-item>
                </el-dropdown-menu>
            </template>
        </el-dropdown>
    </div>
</template>

<script>
export default {
    setup() {


        return {}
    }
}
</script>

<style lang="stylus" scoped>
.modes-wrap {
    padding-top: 0.7rem;
    line-height: normal;

    .el-dropdown-link {
        font-size: 1.2rem;
    }

    .el-icon {
        vertical-align: middle;
    }
}
</style>