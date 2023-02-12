<!--
 * @Author: snltty
 * @Date: 2023-02-10 21:02:51
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 21:48:22
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\ServerLine.vue
-->
<template>
    <div>
        <div class="line flex" :title="$t('home.selectServer')" @click="handleClick">
            <div class="country-img">
                <img :src="shareData.serverImgs[state.item.Img]">
            </div>
            <div class="country-name">{{state.item.Name}}</div>
            <div class="flex-1"></div>
            <div class="country-time">
                <Signal :value="state.pings[0]"></Signal>
            </div>
            <div class="country-select">
                <el-icon>
                    <ArrowRightBold />
                </el-icon>
            </div>
        </div>
    </div>
</template>

<script>
//国旗图片：http://icon.mobanwang.com/2010/467.html
import Signal from '../../components/Signal.vue'
import { injectShareData } from '../../states/shareData'
import { getRegisterInfo, sendPing } from '../../apis/register'
import { onMounted, onUnmounted, reactive } from '@vue/runtime-core';
export default {
    components: { Signal },
    emits: ['handle'],
    setup (props, { emit }) {

        const shareData = injectShareData();
        const state = reactive({
            item: {},
            pings: []
        });

        const update = () => {
            clearTimeout(timer);
            getRegisterInfo().then((info) => {
                let ip = info.ServerConfig.Ip;
                let items = info.ServerConfig.Items.filter(c => c.Ip == ip);
                if (items.length > 0) {
                    state.item = items[0];
                    loadPingData([items[0].Ip]);
                }
            });
        }
        let timer = 0;
        const loadPingData = (ips) => {
            sendPing(ips).then((res) => {
                state.pings = res;
                timer = setTimeout(() => {
                    loadPingData(ips);
                }, 1000);
            });
        }
        onMounted(() => {
            update();
        });
        onUnmounted(() => {
            clearTimeout(timer);
        })

        const handleClick = () => {
            emit('handle');
        }
        return {
            shareData, state, update, handleClick
        }
    }
}
</script>

<style lang="stylus" scoped>
.line {
    cursor: pointer;
    margin: 0 auto;
    border: 1px solid #ddd;
    width: 20rem;
    padding: 0.6rem;
    border-radius: 0.4rem;
    transition: 0.3s;

    &:hover {
        box-shadow: 0 0 0 0.4rem #36836112;
        border-color: #c0d3c9;
    }

    .country-img {
        font-size: 0;
        margin-right: 0.6rem;

        img {
            height: 2rem;
        }
    }

    .country-name {
        line-height: 2rem;
    }

    .country-time, .country-select {
        padding-top: 0.2rem;
    }

    .country-select {
        margin-left: 0.6rem;
        padding-top: 0.3rem;
        color: #999;
    }
}
</style>