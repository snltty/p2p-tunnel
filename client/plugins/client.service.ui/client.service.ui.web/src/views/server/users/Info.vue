<template>
    <div class="wrap" v-if="state.user.ID > 0">
        <el-row>
            <el-col :span="6">
                <el-statistic title="登入" :value="state.user.SignCount"></el-statistic>
                <div class="countdown-footer">{{state.user.SignLimitType == 0 ? '//无限':''}}</div>
            </el-col>
            <el-col :span="6">
                <el-statistic title="流量" :value="state.user.SentBytes">
                    <template #suffix>
                        <span class="suffix">/{{state.user.sentBytes}}</span>
                    </template>
                </el-statistic>
                <div class="countdown-footer">{{state.user.NetFlowType == 0 ? '//无限':`${state.user.NetFlow}/${state.user.netFlow}`}}</div>
            </el-col>
            <el-col :span="6">
                <el-statistic title="权限" :value="state.user.access">
                    <template #suffix>
                        <el-popover placement="top-start" title="权限列表" :width="200" trigger="hover" :content="`【${state.accessText}】`">
                            <template #reference>
                                <span class="suffix">/个<el-icon>
                                        <Warning />
                                    </el-icon></span>
                            </template>
                        </el-popover>
                    </template>
                </el-statistic>
                <div class="countdown-footer">{{state.user.Access == 0 ? '//无':''}}</div>
            </el-col>
            <el-col :span="6">
                <el-statistic title="时间" :value="state.user.endTime">
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
import { shareData } from '../../../states/shareData'
export default {
    setup() {

        const state = reactive({
            user: {
                'ID': 0,
                "Access": 0,
                "access": 0,
                "SignLimit": 0,
                "SignLimitType": 0,
                "SignCount": 0,
                "NetFlowType": 0,
                "SentBytes": 0,
                "sentBytes": 'B',
                "NetFlow": 0,
                'netFlow': 'B',
                "EndTime": '',
                "endTime": 0,
                "_endTime": '',

            },
            accessText: ''
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

        onMounted(() => {
            info().then((res) => {
                let json = JSON.parse(res);
                state.user.ID = json.ID;

                state.user.Access = json.Access;
                state.user.access = json.Access.toString(2).split('').filter(c => c == '1').length;
                state.accessText = shareData.serverAccess.filter(c => shareData.serverAccessHas(state.user.Access, c.value)).map(c => c.text).join('】【')

                state.user.SignLimitType = json.SignLimitType;
                state.user.SignLimit = json.SignLimit;
                state.user.SignCount = json.SignCount;


                state.user.NetFlowType = json.NetFlowType;
                state.user.SentBytes = json.SentBytes;
                let format = json.SentBytes.sizeFormat();
                state.user.SentBytes = format[0];
                state.user.sentBytes = format[1];

                format = json.NetFlow.sizeFormat();
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
    width: 70%;
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