export default [
    {
        text: '运行组网',
        path: 'vea/run',
        params: `略`,
        response: `true //true成功，false失败`
    },
    {
        text: '重启目标节点组网',
        path: 'vea/run',
        params: `1 //目标节点连接id`,
        response: `true //true成功，false失败`
    },
    {
        text: '刷新组网信息',
        path: 'vea/update',
        params: `略`,
        response: `true //true成功，false失败`
    },
    {
        text: '请求在线设备',
        path: 'vea/onlines',
        params: `1 //目标节点连接id`,
        response: `true //true成功，false失败`
    },
    {
        text: '获取在线设备',
        path: 'vea/online',
        params: `1 //目标节点连接id`,
        response: `
{
    Items:{
        //ip,大端的uint
        1:{
            Online:true, //是否在线
            Name:'',    //设备名
        }
    }
}
        `
    },
    {
        text: '节点的组网列表',
        path: 'vea/list',
        params: `略`,
        response: `
//连接id与信息对应，1未某节点的连接id
{
    1:{
        IP:'',      //组网ip
        NetWork:0, //网络号
        Mask:24, //掩码长度
        //局域网段列表
        LanIPs:[
            {
                IPAddress:'', //局域网ip
                Mask:24, //掩码长度
            }
        ]
    }
}
        `
    },
    {
        text: '测试连通',
        path: 'vea/test',
        params: `
{
    Host:'', //ip 或域名，一般是对端组网的ip，或局域网ip
    Port:'', //端口
}
        `,
        response: `
消息编号
Success 0   //成功
Listen 1    //未监听
Address 2   //服务类型未允许
Connection 3//与目标节点未连接

以上为本节点问题
以下为对端节点问题

Denied 4    //目标节点未允许通信
Plugin 5    //目标节点相应插件未找到
EnableOrAccess 6 //目标节点相应插件未允许连接，且未拥有该权限
Firewail 7  //目标节点防火墙阻止
Connect 8   //目标服务连接失败
        `
    },
];