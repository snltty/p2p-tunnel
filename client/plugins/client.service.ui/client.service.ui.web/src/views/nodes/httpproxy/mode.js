import { getListProxy, addListen } from '../../../apis/tcp-forward'

const has = (services) => {
    return services.length == 0 || services.indexOf('HttpProxyClientService') >= 0;
}
const hasNodes = (services) => {
    return services.length == 0 || services.indexOf('ClientsClientService') >= 0;
}

export const remarks = (services) => {
    return [
        `http代理允许连接:${has(services)}`,
        `http代理目标端:${hasNodes(services) ? '手动选择' : '服务器'}`,
    ];
};
export const update = (services) => {
    return new Promise((resolve, reject) => {
        getListProxy().then((json) => {
            json.Name = hasNodes(services) ? '' : '/';
            addListen(json).then(resolve).catch(reject);
        }).catch(reject)
    });
}