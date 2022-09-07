<!--
 * @Author: snltty
 * @Date: 2021-08-19 21:50:16
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-16 13:00:43
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\views\home\Counter.vue
-->
<template>
    <div class="counter-wrap">
        <h3 class="title t-c">服务器信息</h3>
        <div class="content">
            <el-row>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">time : </span><span class="value">{{RunTime}}</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">cpu : </span><span class="value">{{Cpu}}</span><span class="text">%</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">memory : </span><span class="value">{{Memory}}</span><span class="text">MB</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">online : </span><span class="value">{{OnlineCount}}</span>
                        </span>
                    </div>
                </el-col>

                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">tcp send : </span><span class="value">{{tcp.send.bytes}}</span><span class="text">{{tcp.send.bytesUnit}}</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">tcp send : </span><span class="value">{{tcp.send.bytesSec}}</span><span class="text">{{tcp.send.bytesSecUnit}}/s</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">tcp receive : </span><span class="value">{{tcp.receive.bytes}}</span><span class="text">{{tcp.receive.bytesUnit}}</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">tcp receive : </span><span class="value">{{tcp.receive.bytesSec}}</span><span class="text">{{tcp.receive.bytesSecUnit}}/s</span>
                        </span>
                    </div>
                </el-col>

                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">udp send : </span><span class="value">{{udp.send.bytes}}</span><span class="text">{{udp.send.bytesUnit}}</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">udp send : </span><span class="value">{{udp.send.bytesSec}}</span><span class="text">{{udp.send.bytesSecUnit}}/s</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">udp receive : </span><span class="value">{{udp.receive.bytes}}</span><span class="text">{{udp.receive.bytesUnit}}</span>
                        </span>
                    </div>
                </el-col>
                <el-col :xs="8" :sm="6" :md="6" :lg="6" :xl="6">
                    <div class="col">
                        <span class="box">
                            <span class="text">udp receive : </span><span class="value">{{udp.receive.bytesSec}}</span><span class="text">{{udp.receive.bytesSecUnit}}/s</span>
                        </span>
                    </div>
                </el-col>
            </el-row>
        </div>
    </div>
</template>

<script>
import { reactive, toRefs } from '@vue/reactivity'
import { getCounter } from '../../apis/counter'
import { onUnmounted } from '@vue/runtime-core'
export default {
    name: 'Counter',
    components: {},
    setup () {

        const state = reactive({
            OnlineCount: 0,
            Cpu: 0,
            Memory: 0,
            RunTime: 0,

            tcp: {
                send: {
                    bytes: 0,
                    bytesUnit: 0,
                    _bytes: 0,
                    bytesSec: 0,
                    bytesSecUnit: 0,
                },
                receive: {
                    bytes: 0,
                    bytesUnit: 0,
                    _bytes: 0,
                    bytesSec: 0,
                    bytesSecUnit: 0,
                }
            },
            udp: {
                send: {
                    bytes: 0,
                    bytesUnit: 0,
                    _bytes: 0,
                    bytesSec: 0,
                    bytesSecUnit: 0,
                },
                receive: {
                    bytes: 0,
                    bytesUnit: 0,
                    _bytes: 0,
                    bytesSec: 0,
                    bytesSecUnit: 0,
                }
            }
        });


        const getSendBytesOption = () => {
            return {
                title: {
                    text: '',
                    left: 'center'
                },
                tooltip: {
                    trigger: 'item'
                },
                series: [
                    {
                        name: '服务器已发送数据',
                        type: 'pie',
                        radius: '60%',
                        data: [
                            { value: state.tcp.send._bytes, name: 'TCP' },
                            { value: state.udp.send._bytes, name: 'UDP' },
                        ],
                        emphasis: {
                            itemStyle: {
                                shadowBlur: 10,
                                shadowOffsetX: 0,
                                shadowColor: 'rgba(0, 0, 0, 0.5)'
                            }
                        }
                    }
                ]
            }
        }
        const updateData = () => {
            getCounter().then((res) => {
                if (res) {
                    const json = res;
                    state.OnlineCount = json.OnlineCount;
                    state.Cpu = json.Cpu;
                    state.Memory = json.Memory;
                    state.RunTime = json.RunTime.timeFormat().join(':');

                    let format;

                    format = json.TcpSendBytes.sizeFormat();
                    state.tcp.send.bytes = format[0];
                    state.tcp.send.bytesUnit = format[1];
                    state.tcp.send.bytesSec = json.TcpSendBytes - state.tcp.send._bytes;
                    format = state.tcp.send.bytesSec.sizeFormat();
                    state.tcp.send.bytesSec = format[0];
                    state.tcp.send.bytesSecUnit = format[1];
                    state.tcp.send._bytes = json.TcpSendBytes;

                    format = json.TcpReceiveBytes.sizeFormat();
                    state.tcp.receive.bytes = format[0];
                    state.tcp.receive.bytesUnit = format[1];
                    state.tcp.receive.bytesSec = json.TcpReceiveBytes - state.tcp.receive._bytes;
                    format = state.tcp.receive.bytesSec.sizeFormat();
                    state.tcp.receive.bytesSec = format[0];
                    state.tcp.receive.bytesSecUnit = format[1];
                    state.tcp.receive._bytes = json.TcpReceiveBytes;


                    format = json.UdpSendBytes.sizeFormat();
                    state.udp.send.bytes = format[0];
                    state.udp.send.bytesUnit = format[1];
                    state.udp.send.bytesSec = json.UdpSendBytes - state.udp.send._bytes;
                    format = state.udp.send.bytesSec.sizeFormat();
                    state.udp.send.bytesSec = format[0];
                    state.udp.send.bytesSecUnit = format[1];
                    state.udp.send._bytes = json.UdpSendBytes;

                    format = json.UdpReceiveBytes.sizeFormat();
                    state.udp.receive.bytes = format[0];
                    state.udp.receive.bytesUnit = format[1];
                    state.udp.receive.bytesSec = json.UdpReceiveBytes - state.udp.receive._bytes;
                    format = state.udp.receive.bytesSec.sizeFormat();
                    state.udp.receive.bytesSec = format[0];
                    state.udp.receive.bytesSecUnit = format[1];
                    state.udp.receive._bytes = json.UdpReceiveBytes;
                }
            }).catch(() => {
            });
        }
        const timer = setInterval(updateData, 1000);
        onUnmounted(() => {
            clearInterval(timer);
        });

        return {
            ...toRefs(state)
        }
    }
}
</script>
<style lang="stylus" scoped>
.line-col
    height: 20rem;

    &.bytes
        width: 20rem;

.counter-wrap
    padding: 2rem;
    margin-top: 2rem;
    border: 1px solid #eee;
    border-radius: 0.4rem;

    .content
        margin-top: 2rem;

@media screen and (max-width: 600px)
    .el-col-xs-8
        max-width: 50%;
        flex: 0 0 50%;

@media screen and (max-width: 450px)
    .counter-wrap
        padding: 2rem 0.6rem 1rem 0.6rem;

        .content
            margin-top: 1rem;

div.col
    // text-align: center;
    padding: 0.6rem 0.6rem;
    color: #2c7e63;

    span
        display: inline-block;
        padding: 0.4rem;

    span.box
        background-color: #eee;
        border-radius: 0.4rem;
        display: block;

        span.value
            background-color: #ccc;
            border-radius: 0.4rem;
            padding: 0 0.4rem;
            color: #1f666a;
            font-weight: bold;
</style>