<template>
    <div>
        <div class="line flex" title="选择服务器线路" @click="handleClick">
            <div class="country-img">
                <img :src="shareData.serverImgs[state.item.Img].img">
            </div>
            <div class="country-name">{{shareData.serverImgs[state.item.Img].name}}<span v-if="state.item.Name">-{{state.item.Name}}</span></div>
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
import { getSignInInfo, sendPing } from '../../apis/signin'
import { onMounted, onUnmounted, reactive } from '@vue/runtime-core';
export default {
    components: { Signal },
    emits: ['handle'],
    setup(props, { emit }) {

        const shareData = injectShareData();
        const state = reactive({
            item: { Img: 'zg', Name: '未知', Ip: '127.0.0.1' },
            pings: [-1]
        });

        const update = () => {
            clearTimeout(timer);
            getSignInInfo().then((info) => {
                let ip = info.ServerConfig.Ip;
                let items = info.ServerConfig.Items.filter(c => c.Ip == ip);
                if (items.length > 0) {
                    state.item = items[0];
                } else {
                    state.item.Ip = ip;
                }
                loadPingData([state.item.Ip]);
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
    line-height: normal;
    font-size: 1.2rem;
    cursor: pointer;
    margin: 0 auto;
    border: 1px solid #ddd;
    width: 20rem;
    padding: 0.6rem;
    border-radius: 0.4rem;
    transition: 0.3s;
    background-color: #fff;

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
        color: #666;
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