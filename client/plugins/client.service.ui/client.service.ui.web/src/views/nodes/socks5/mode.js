import { get, set } from '../../../apis/socks5'

const has = (services, name) => {
    return (services.length == 0 || services.indexOf('Socks5ClientService') >= 0) && name != 'proxy';
}
const hasNodes = (services, name) => {
    return services.length == 0 || services.indexOf('ClientsClientService') >= 0;
}

export const remarks = (services, name) => {
    return [
        `socks5允许连接:${has(services, name)}`,
        `socks5目标端:${hasNodes(services, name) ? '手动选择' : '服务器'}`,
    ];
};
export const update = (services, name) => {
    return new Promise((resolve, reject) => {
        get().then((json) => {
            json.ConnectEnable = has(services, name);
            json.TargetName = hasNodes(services, name) ? '' : '/';
            set(json).then(resolve).catch(reject);
        }).catch(reject)
    });
}