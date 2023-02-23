import { getRegisterInfo, updateConfig } from '../../../apis/register'

const hasNodes = (services) => {
    return services.length == 0 || services.indexOf('ClientsClientService') >= 0;
}
export const remarks = (services) => {
    return [
        `自动注册:true`,
        `原端口打洞:${hasNodes(services)}`,
        `使用tcp:true，使用udp:${hasNodes(services)}`,
        `自动打洞:${hasNodes(services)}，打洞断线重连:${hasNodes(services)}`,
        `自动中继:${hasNodes(services)}，中继节点:${hasNodes(services)}`,
    ];
};
export const update = (services) => {
    return new Promise((resolve, reject) => {
        getRegisterInfo().then((json) => {
            let _hasNodes = hasNodes(services);
            json.ClientConfig.AutoReg = true;
            json.ClientConfig.UseUdp = _hasNodes;
            json.ClientConfig.UseOriginPort = _hasNodes;
            json.ClientConfig.UsePunchHole = _hasNodes;
            json.ClientConfig.UseReConnect = _hasNodes;
            json.ClientConfig.AutoRelay = _hasNodes;
            json.ClientConfig.UseRelay = _hasNodes;

            updateConfig(json).then(resolve).catch(reject);
        }).catch(reject)
    });
}