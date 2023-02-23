
import { getConfig as getTcpConfig, updateConfig as updateTcpConfig } from '../../apis/tcp-forward'
import { getConfig as getUdpConfig, updateConfig as updateUdpConfig } from '../../apis/udp-forward'

const updateTcp = (services) => {
    return new Promise((resolve, reject) => {
        getTcpConfig().then((json) => {
            json = new Function("return" + json)();
            json.ConnectEnable = hasTcp(services);
            updateTcpConfig(JSON.stringify(json, null, 4)).then(resolve).catch(reject);
        }).catch(reject)
    });
}
const updateUdp = (services) => {
    return new Promise((resolve, reject) => {
        getUdpConfig().then((json) => {
            json = new Function("return" + json)();
            json.ConnectEnable = hasUdp(services);
            updateUdpConfig(JSON.stringify(json, null, 4)).then(resolve).catch(reject);
        }).catch(reject)
    });
}
const hasTcp = (services) => {
    return services.length == 0
        || services.indexOf('ServerTcpForwardClientService') >= 0
        || services.indexOf('TcpForwardClientService') >= 0
}
const hasUdp = (services) => {
    return services.length == 0
        || services.indexOf('ServerUdpForwardClientService') >= 0
        || services.indexOf('UdpForwardClientService') >= 0;
}

export const remarks = (services) => {
    return [
        `tcp转发允许连接:${hasTcp(services)}`,
        `udp转发允许连接:${hasUdp(services)}`,
    ];
};
export const update = (services) => {
    return Promise.all([updateTcp(services), updateUdp(services)]);
}