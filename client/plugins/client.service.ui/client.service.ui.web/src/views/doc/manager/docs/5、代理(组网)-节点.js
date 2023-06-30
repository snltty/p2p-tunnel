export default [
    {
        text: '配置信息',
        path: '',
        params: `//直接去使用配置文件接口修改配置即可`,
        response: `略`
    },
    {
        text: '运行组网',
        path: 'vea/run',
        params: `略 //配置文件中有一个 ListenEnable 当为true时，此接口开启组网，false时此接口关闭组网`,
        response: `true //true成功，false失败`
    },
    {
        text: '重启组网',
        path: 'vea/run',
        params: `1 //目标节点连接id`,
        response: `true //true成功，false失败`
    },
    {
        text: '刷新组网信息',
        path: 'vea/update',
        params: `略 //向各个节点请求组网信息，本来是自动的，但是也会有失败的时候，就可以用此接口手动刷新`,
        response: `true //true成功，false失败`
    },
    {
        text: '组网信息列表',
        path: 'vea/list',
        params: `略`,
        response: `
//当获取不到信息时，可以调用  vea/update 刷新一下
//连接id与信息对应，1未某节点的连接id
{
    1:{
        IP:'',      //组网ip
        NetWork:0, //大端uint，网络号
        Mask:24, //byte 掩码长度
        //局域网段列表
        LanIPs:[
            {
                IPAddress:'', //局域网ip
                Mask:24, //byte 掩码长度
            }
        ]
    }
}
        `
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
        params: `1 //目标节点连接id //当获取不到时，可以调用 vea/onlines 刷新一下`,
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