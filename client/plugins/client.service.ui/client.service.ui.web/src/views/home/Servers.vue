<!--
 * @Author: snltty
 * @Date: 2023-02-10 17:33:52
 * @LastEditors: snltty
 * @LastEditTime: 2023-02-12 01:51:03
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Servers.vue
-->
<template>
    <div class="servers-mark absolute" :class="{show:state.animation}" @click="handleClose" ref="rootDom">
        <div class="servers-wrap absolute scrollbar" @click.stop>
            <ul>
                <template v-for="(item,index) in state.servers" :key="index">
                    <li class="flex" @click="handleSelect(item)">
                        <div class="country-img">
                            <img :src="shareData.serverImgs[item.Img]">
                        </div>
                        <div class="country-name">{{item.Name}}</div>
                        <div class="flex-1"></div>
                        <div class="country-time">
                            <Signal :value="state.pings[index] || -1"></Signal>
                        </div>
                    </li>
                </template>
            </ul>
        </div> 
    </div>
</template>  
   
<script>
import { onMounted, onUnmounted, reactive, ref, watch } from '@vue/runtime-core'
import Signal from '../../components/Signal.vue'
import {getRegisterInfo,sendPing,updateConfig} from '../../apis/register'
import { ElLoading } from 'element-plus'
import { ElMessage } from 'element-plus/lib/components'
import {injectShareData} from '../../states/shareData'
export default {
    components:{Signal},
    props: ['modelValue'],
    emits: ['update:modelValue', 'success'],
    setup (props, { emit }) {

        const shareData = injectShareData();
        const state = reactive({
            configInfo:{},
            servers:[],
            pings:[],
            animation:false
        })
        watch(() => state.animation, (val) => {
            if (val == false) {
                setTimeout(() => {
                    emit('update:modelValue', val);
                }, 300);
            }
        });

        const loadData = ()=>{
            getRegisterInfo().then((info)=>{
                state.configInfo = info;
                state.servers = info.ServerConfig.Items;
                loadPingData(state.servers.map(c=>c.Ip));
            });
        }

        let timer = 0;
        const loadPingData = (ips)=>{
            sendPing(ips).then((res)=>{
                state.pings = res;
                timer = setTimeout(()=>{
                    loadPingData(ips);
                },1000);
            });
        }
        onMounted(()=>{
            loadData();
            setTimeout(()=>{
                state.animation = true;
            });
        });
        onUnmounted(()=>{
            clearTimeout(timer);
        })

        const handleClose = ()=>{
            if(loadingInstance != null) return;
            state.animation = false;
            emit('success');
        }

        const rootDom  = ref(null);
        let loadingInstance = null;
        const handleSelect = (item)=>{
            loadingInstance = ElLoading.service({ target:rootDom.value });
            state.configInfo.ServerConfig.Ip = item.Ip;
            state.configInfo.ServerConfig.UdpPort = item.UdpPort;
            state.configInfo.ServerConfig.TcpPort = item.TcpPort;
            updateConfig(state.configInfo).then(()=>{
                loadingInstance.close();
                loadingInstance = null;
                handleClose();
            }).catch(()=>{
                loadingInstance.close();
                loadingInstance = null;
                ElMessage.error('选择失败');
            })
        }
 
        return {shareData,state,handleClose,handleSelect,rootDom}
    }
}
</script>

<style lang="stylus" scoped>
.servers-mark
    background-color: rgba(0,0,0,0.05);
    &.show
        .servers-wrap
            left: 100%;
            transform: translateX(-20rem);
.servers-wrap
    left: 100%;
    transform: translateX(0);
    right: auto;
    width: 20rem;
    border-left: 1px solid #ddd;
    box-shadow: -1px -1px .6rem rgba(0,0,0,0.05);
    background-color: #fff;
    transition: .3s cubic-bezier(.56,-0.37,.78,1.66);


    ul
        padding: 1rem
li
    cursor: pointer;
    margin: 0 auto;
    border: 1px solid #ddd;
    width: 100%;
    padding: 0.6rem;
    border-radius: 0.4rem;
    transition: 0.3s;
    box-sizing: border-box;
    margin-bottom: 1rem

    &:hover
        box-shadow: 0 0 0 0.4rem #d1d8e261;

    .country-img
        font-size: 0;
        margin-right: 0.6rem;

        img
            height: 2rem;

    .country-name
        line-height: 2rem;
        color: #555

    .country-time
        padding-top: 0.2rem;
</style>