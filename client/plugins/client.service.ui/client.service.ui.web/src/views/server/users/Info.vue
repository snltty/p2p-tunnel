<template>
    <div class="wrap" v-if="state.user.ID > 0">
        <el-row>
            <el-col :span="6">
                <el-statistic title="最多登入数" :value="state.user.SignLimit"></el-statistic>
                <div class="countdown-footer">{{state.user.SignLimit == -1 ? '//无限制':''}}</div>
            </el-col>
            <el-col :span="6">
                <el-statistic title="剩余流量" :value="state.user.NetFlow">
                    <template #suffix>
                        <template v-if="state.user.NetFlow != -1"><span class="suffix">/{{state.user.netFlow}}</span></template>
                    </template>
                </el-statistic>
                <div class="countdown-footer">{{state.user.NetFlow == -1 ? '//无限制':''}}</div>
            </el-col>
            <el-col :span="6">
                <el-statistic title="权限" :value="state.user.access">
                    <template #suffix><span class="suffix">/个</span></template>
                </el-statistic>
                <div class="countdown-footer">{{state.user.Access == 0 ? '//无权限':''}}</div>
            </el-col>
            <el-col :span="6">
                <el-statistic title="剩余时间" :value="state.user.endTime">
                    <template #suffix><span class="suffix">/{{state.user._endTime}}</span></template>
                </el-statistic>
                <div class="countdown-footer">{{state.user.EndTime}}</div>
            </el-col>
        </el-row>
    </div>
</template>

<script>
import { onMounted, reactive } from 'vue'
import { info } from '../../../apis/users-server'
export default {
    setup() {

        const state = reactive({
            user: {
                'ID': 0,
                "Access": 0,
                "access": 0,
                "SignLimit": -1,
                "NetFlow": -1,
                'netFlow': 'B',
                "EndTime": '',
                "endTime": 0,
                "_endTime": '',

            }
        });

        const timeFormat = (num) => {
            if (num <= 0) {
                num = 0;
            }

            let nums = [1000, 60, 60, 24, 365];
            let txts = ['秒', '分', '时', '天', '年'];

            let index = 0;
            for (index = 0; index < nums.length; index++) {
                if (Math.ceil(num) < nums[index]) {
                    break;
                }
                num /= nums[index];
            }
            return [num, txts[index - 1]];
        }
        const accessLength = (access) => {
            let length = 0;
            while (access > 0) {
                length++;
                access = access >> 1;
            }
            return length;
        }

        onMounted(() => {
            info().then((res) => {
                let json = JSON.parse(res);
                state.user.ID = json.ID;

                let length = accessLength(json.Access);
                state.user.Access = json.Access;
                state.user.access = length;

                state.user.SignLimit = json.SignLimit;

                let format = json.NetFlow.sizeFormat();
                state.user.NetFlow = format[0];
                state.user.netFlow = format[1];

                format = timeFormat(new Date(json.EndTime).getTime() - new Date().getTime());
                state.user.EndTime = new Date(json.EndTime).format('yyyy-MM-dd');
                state.user.endTime = Math.floor(format[0]);
                state.user._endTime = format[1];
            });
        });




        return {
            state
        }
    }
}
</script>

<style lang="stylus" scoped>
.wrap {
    width: 50%;
    margin: 0 auto;
    border: 1px solid #ddd;
    border-radius: 0.4rem;

    .el-col {
        padding: 1rem;
        box-sizing: border-box;
    }

    .suffix {
        font-size: 1.2rem;
    }
}

.countdown-footer {
    font-size: 1.2rem;
}
</style>