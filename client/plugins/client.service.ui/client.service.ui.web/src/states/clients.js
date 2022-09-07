/*
 * @Author: snltty
 * @Date: 2021-08-21 14:57:33
 * @LastEditors: snltty
 * @LastEditTime: 2022-08-31 23:08:18
 * @version: v1.0.0
 * @Descripttion: 功能说明
 * @FilePath: \client.service.ui.web\src\states\clients.js
 */
import { provide, inject, reactive } from "vue";
import { websocketState } from '../apis/request'
import { getClients } from '../apis/clients'

const connectTypeStrs = ['未连接', '已连接-打洞', '已连接-中继'];
const connectTypeColors = ['color:#333;', 'color:#148727;font-weight:bold;', 'color:#148727;font-weight:bold;'];

const provideClientsKey = Symbol();
export const provideClients = () => {
    const state = reactive({
        clients: []
    });
    provide(provideClientsKey, state);

    setInterval(() => {
        if (websocketState.connected) {
            getClients().then((res) => {
                res.forEach(c => {
                    c.udpConnectType = c.UdpConnected ? c.UdpConnectType : Number(c.UdpConnected);
                    c.tcpConnectType = c.TcpConnected ? c.TcpConnectType : Number(c.TcpConnected);

                    c.udpConnectTypeStr = connectTypeStrs[c.udpConnectType];
                    c.udpConnectTypeStyle = connectTypeColors[c.udpConnectType];

                    c.tcpConnectTypeStr = connectTypeStrs[c.tcpConnectType];
                    c.tcpConnectTypeStyle = connectTypeColors[c.tcpConnectType];

                    c.connectDisabled = false;
                    if (c.Udp || c.Tcp) {
                        c.connectDisabled = c.UdpConnected && c.TcpConnected;
                    } else if (c.Udp) {
                        c.connectDisabled = c.UdpConnected;
                    } else if (c.Tcp) {
                        c.connectDisabled = c.TcpConnected;
                    }

                    c.online = c.UdpConnected || c.TcpConnected;
                });
                state.clients = res;
            })
        } else {
            state.clients = [];
        }
    }, 1000);
}
export const injectClients = () => {
    return inject(provideClientsKey);
}