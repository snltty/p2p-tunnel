import { inject, provide, reactive } from "vue";

const files = require.context('../views/', true, /plugin(-[a-zA-Z0-9]+)?\.js/);
const accesss = files.keys().map(c => files(c).default).filter(c => c.access > 0).reduce((all, value, index) => {
    all.push({
        text: value.text,
        value: value.access
    })
    return all;
}, [
    { text: '中继', value: 1 }
]);

export const shareData = {
    aliveTypes: { 0: '长连接', 1: '短链接' },
    aliveTypesName: { "tunnel": 0, 'web': 1 },
    forwardTypes: { 'forward': 0, 'proxy': 1 },
    clientConnectTypes: { 0: '未连接', 1: '打洞', 2: '节点中继', 4: '服务器中继' },
    serverTypes: { 1: 'TCP', 2: 'UDP', 3: '/' },
    serverAccess: accesss,
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