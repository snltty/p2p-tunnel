import { inject, provide, reactive } from "vue";

const shareDataKey = Symbol();

export const shareData = {
    aliveTypes: { 0: '长连接', 1: '短链接' },
    aliveTypesName: { "tunnel": 0, 'web': 1 },
    forwardTypes: { 'forward': 0, 'proxy': 1 },
    clientConnectTypes: { 0: '未连接', 1: '打洞', 2: '节点中继', 4: '服务器中继' },
    serverTypes: { 1: 'TCP', 2: 'UDP', 3: '/' },
    serverAccess: {
        'signin': { text: '登入', value: 1 },
        'relay': { text: '中继', value: 2 },
        'tcpforward': { text: 'tcp转发', value: 4 },
        'udpforward': { text: 'udp转发', value: 8 },
        'socks5': { text: 'socks5代理', value: 16 },
        'setting': { text: '服务器配置', value: 32 },
    },
    serverAccessHas: (access, value) => {
        return (access & value) == value;
    },
    serverAccessHasSignin: (access) => {
        return shareData.serverAccessHas(access, shareData.serverAccess.signin);
    },
    serverAccessHasRelay: (access) => {
        return shareData.serverAccessHas(access, shareData.serverAccess.relay);
    },
    serverAccessHasTcpForward: (access) => {
        return shareData.serverAccessHas(access, shareData.serverAccess.tcpforward);
    },
    serverAccessHasUdpForward: (access) => {
        return shareData.serverAccessHas(access, shareData.serverAccess.udpforward);
    },
    serverAccessHasSocks5: (access) => {
        return shareData.serverAccessHas(access, shareData.serverAccess.socks5);
    },
    serverAccessHasSetting: (access) => {
        return shareData.serverAccessHas(access, shareData.serverAccess.setting);
    },
    serverImgs: {
        'zg': { img: require('../assets/zg.png'), name: '中国' },
        'zgxg': { img: require('../assets/zgxg.png'), name: '中国香港' },
        'xjp': { img: require('../assets/xjp.png'), name: '新加坡' },
        'hg': { img: require('../assets/hg.png'), name: '韩国' },
        'rb': { img: require('../assets/rb.png'), name: '日本' },
        'mg': { img: require('../assets/mg.png'), name: '美国' },
    }
};

export const provideShareData = () => {
    const state = reactive(shareData);
    provide(shareDataKey, state);
}
export const injectShareData = () => {
    return inject(shareDataKey);
}