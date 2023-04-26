<template>
    <el-form-item label="" label-width="0">
        <el-row style="width:100%">
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                <el-form-item label="账号" prop="Account">
                    <el-tooltip class="box-item" effect="dark" content="账号" placement="top-start">
                        <el-input type="password" size="default" v-model="state.account" placeholder="账号" show-password></el-input>
                    </el-tooltip>
                </el-form-item>
            </el-col>
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
                <el-form-item label="密码" prop="Password">
                    <el-tooltip class="box-item" effect="dark" content="密码" placement="top-start">
                        <el-input type="password" size="default" placeholder="密码" v-model="state.password" show-password></el-input>
                    </el-tooltip>
                </el-form-item>
            </el-col>
        </el-row>
    </el-form-item>
</template>

<script>
import { reactive, watch } from 'vue'
import plugin from './plugin'
export default {
    plugin: plugin,
    props: {
        args: {
            type: Object,
            default: {}
        }
    },
    setup(props) {

        const state = reactive({
            account: props.args.account,
            password: props.args.password,
        });
        watch(() => props.args, () => {
            state.account = props.args.account;
            state.password = props.args.password;
        });

        const submit = () => {
            return {
                account: state.account,
                password: state.password,
            }
        }

        return {
            state, submit
        }
    }
}
</script>

<style lang="scss" scoped>
</style>