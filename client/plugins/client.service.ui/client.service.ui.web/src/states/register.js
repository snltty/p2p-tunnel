/*
 * @Author: snltty
 * @Date: 2021-08-19 22:39:45
 * @LastEditors: snltty
 * @LastEditTime: 2022-09-01 17:12:27
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\states\register.js
 */
// import { provide, inject, reactive } from 'vue'

import { provide, inject, reactive } from "vue";
import { websocketState } from '../apis/request'
import { getRegisterInfo } from '../apis/register'

const provideRegisterKey = Symbol();
export const provideRegister = () => {
    const state = reactive({
        ClientConfig: {
            GroupId: '',
            Name: '',
            AutoReg: false,
            AutoRegTimes: 10,
            AutoRegInterval: 5000,
            AutoRegDelay: 5000,
            UseMac: false,
            Encode: false,
            EncodePassword: "",
            AutoPunchHole: false,
            UseUdp: false,
            UseTcp: false,
        },
        ServerConfig: {
            Ip: '',
            UdpPort: 0,
            TcpPort: 0,
            Encode: false,
            EncodePassword: ""
        },
        LocalInfo: {
            RouteLevel: 0,
            Mac: '',
            Port: 0,
            TcpPort: 0,
            IsConnecting: false,
            UdpConnected: false,
            TcpConnected: false,
            LocalIp: ''
        },
        RemoteInfo: {
            Ip: '',
            UdpPort: 0,
            TcpPort: 0,
            ConnectId: 0,
        }
    });
    provide(provideRegisterKey, state);

    setInterval(() => {
        if (websocketState.connected) {
            getRegisterInfo().then((json) => {
                state.LocalInfo.UdpConnected = json.LocalInfo.UdpConnected;
                state.LocalInfo.TcpConnected = json.LocalInfo.TcpConnected;
                state.LocalInfo.UdpPort = json.LocalInfo.UdpPort;
                state.LocalInfo.TcpPort = json.LocalInfo.TcpPort;
                state.LocalInfo.Mac = json.LocalInfo.Mac;
                state.LocalInfo.LocalIp = json.LocalInfo.LocalIp;

                state.LocalInfo.connected = state.LocalInfo.UdpConnected || state.LocalInfo.TcpConnected;

                state.ClientConfig.UseUdp = json.ClientConfig.UseUdp;
                state.ClientConfig.UseTcp = json.ClientConfig.UseTcp;

                state.RemoteInfo.UdpPort = json.RemoteInfo.UdpPort;
                state.RemoteInfo.TcpPort = json.RemoteInfo.TcpPort;
                state.RemoteInfo.Ip = json.RemoteInfo.Ip;
                state.RemoteInfo.ConnectId = json.RemoteInfo.ConnectId;

                state.LocalInfo.IsConnecting = json.LocalInfo.IsConnecting;
                state.LocalInfo.RouteLevel = json.LocalInfo.RouteLevel;
                if (!state.ClientConfig.GroupId) {
                    state.ClientConfig.GroupId = json.ClientConfig.GroupId;
                }
                if (!state.ServerConfig.Ip) {
                    state.ServerConfig.Ip = json.ServerConfig.Ip;
                    state.ServerConfig.UdpPort = json.ServerConfig.UdpPort;
                    state.ServerConfig.TcpPort = json.ServerConfig.TcpPort;
                }
            })
        } else {
            state.UdpConnected = false;
            state.TcpConnected = false;
        }
    }, 300);
}
export const injectRegister = () => {
    return inject(provideRegisterKey);
}