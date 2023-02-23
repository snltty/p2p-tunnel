import { getConfig, setConfig } from '../../../apis/vea'

const has = (services) => {
    return services.length == 0 || services.indexOf('VeaClientService') >= 0;
}
export const remarks = (services) => {
    return [
        `虚拟网卡组网允许连接:${has(services)}`
    ];
};
export const update = (services) => {
    return new Promise((resolve, reject) => {
        getConfig().then((json) => {
            json.ConnectEnable = has(services);
            setConfig(json).then(resolve).catch(reject);
        }).catch(reject)
    });
}