export default [
    {

        text: '获取节点列表',
        path: 'clients/list',
        params: `略`,
        response: `
[
    {
        Connecting:true,        //连接中
        Connected:true,         //已连接
        IPAddress:'127.0.0.1',
        Name:'B',               //节点名
        ConnectionId:1,         //连接id
        ClientAccess:0,         //配置权限
        UserAccess:0,           //服务器权限
        Ping:0,                 //延迟，-1超时
        ServerType:1,           //连接类别，1tcp，2udp
        ConnectType:0,          //连接类型，0未连接，1p2p，2节点中继，3服务器中继
        OnlineType:1,           //上线类型，0未知，1主动方，2被动方
        OfflineType:2,          //离线类型，0未知，1被动，2，主动
    }
]
        `
    },
    {
        text: '连接节点',
        path: 'clients/connect',
        params: `1 //连接id`,
        response: `true //true成功，false失败`
    },
    {
        text: '反向连接节点',
        path: 'clients/connectReverse',
        params: `1 //连接id`,
        response: `true //true成功，false失败`,
    },
    {
        text: '重启节点',
        path: 'clients/reset',
        params: `1 //连接id`,
        response: `true //true成功，false失败`
    },
    {
        text: '断开节点',
        path: 'clients/offline',
        params: `1 //连接id`,
        response: `true //true成功，false失败`
    },
    {
        text: 'ping',
        path: 'clients/ping',
        params: `略`,
        response: `略 //会去ping所有节点，更新到节点信息的Ping中，再次获取节点列表即可`
    },
    {
        text: '节点连接情况',
        path: 'clients/connects',
        params: `略`,
        response: `
{
    1:[2,3,4] //key表示节点，value为此节点连接了哪些节点
}
        `
    },
    {
        text: '节点线路延迟情况',
        path: 'clients/delay',
        params: `[][] //多个线路，每个线路都是id列表 `,
        response: `[] //按顺序，每个线路的延迟 -1超时`
    },
    {
        text: '中继',
        path: 'clients/relay',
        params: `[1,2,3,4] //线路，1通过2,3中继到4，也就是第一项为来源节点，最后一项为目标节点，中间为中间节点`,
        response: `true //true成功，false失败`
    }
]