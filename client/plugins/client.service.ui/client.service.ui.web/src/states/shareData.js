import { inject, provide, reactive } from "vue";

const files = require.context('../views/server/', true, /plugin(-[a-zA-Z0-9]+)?\.js/);
const filesClient = require.context('../views/nodes/', true, /plugin(-[a-zA-Z0-9]+)?\.js/);
const serverPlugins = files.keys().map(c => files(c).default);
const clientPlugins = filesClient.keys().map(c => filesClient(c).default);

const serverAccesss = serverPlugins.filter(c => c.access > 0).reduce((all, value, index) => {
    all.push({
        text: value.accessText || value.text,
        value: value.access
    });
    return all;
}, []);
const clientAccess = clientPlugins.filter(c => c.access > 0).reduce((all, value, index) => {
    all.push({
        text: value.accessText || value.text,
        value: value.access
    });
    return all;
}, []);

const serverProxys = serverPlugins.filter(c => c.proxyId > 0).reduce((all, value, index) => {
    all.push({
        text: value.text,
        value: value.proxyId,
        local: value.local
    });
    return all;
}, []);
const clientProxys = clientPlugins.filter(c => c.proxyId > 0).reduce((all, value, index) => {
    all.push({
        text: value.text,
        value: value.proxyId,
        local: value.local
    });
    return all;
}, []);


export const shareData = {
    aliveTypes: { 0: '长连接', 1: '短链接' },
    aliveTypesName: { "tunnel": 0, 'web': 1 },
    clientConnectTypes: { 0: '未连接', 1: '打洞', 2: '节点中继', 4: '服务器中继' },
    serverTypes: { 1: 'TCP', 2: 'UDP', 3: '/' },
    bufferSizes: ['KB_1', 'KB_2', 'KB_4', 'KB_8', 'KB_16', 'KB_32', 'KB_64', 'KB_128', 'KB_256', 'KB_512', 'KB_1024'],
    commandMsgs: [
        '最后一次通信的错误情况',
        '服务类型允许',
        '与目标节点已连接',
        '目标节点允许通信',
        '目标节点存在对应插件',
        '插件允许连接或拥有权限',
        '目标节点防火墙允许',
        '目标服务连接成功'
    ],
    clientAccess: clientAccess,
    serverAccess: serverAccesss,
    serverProxys: serverProxys,
    clientProxys: clientProxys,
    serverImgs: {
        'zg': { img: require('../assets/zg.png'), name: '中国' },
        'zgxg': { img: require('../assets/zgxg.png'), name: '中国香港' },
        'xjp': { img: require('../assets/xjp.png'), name: '新加坡' },
        'hg': { img: require('../assets/hg.png'), name: '韩国' },
        'rb': { img: require('../assets/rb.png'), name: '日本' },
        'mg': { img: require('../assets/mg.png'), name: '美国' },
    },
    serverAccessHas: (access, value) => {
        return (((access >>> 0) & (value >>> 0)) >>> 0) == value;
    },
    serverAccessDel: (access, value) => {
        return (((access >>> 0) & (~value >>> 0)) >>> 0);
    },
    serverAccessAdd: (access, value) => {
        return (((access >>> 0) | (value >>> 0)) >>> 0);
    },
    serverAccessHasRelay: (access) => {
        return shareData.serverAccessHas(access, 1);
    }
};

const shareDataKey = Symbol();
export const provideShareData = () => {
    const state = reactive(shareData);
    provide(shareDataKey, state);
}
export const injectShareData = () => {
    return inject(shareDataKey);
}