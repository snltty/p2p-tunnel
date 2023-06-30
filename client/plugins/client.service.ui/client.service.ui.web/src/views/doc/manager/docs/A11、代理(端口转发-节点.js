export default [
    {
        text: '配置信息',
        path: '',
        params: `//直接去使用配置文件接口修改配置即可`,
        response: `略`
    },
    {
        text: '监听列表',
        path: 'forward/list',
        params: `略`,
        response: `
[
    {
        ID:1,//编号
        Port:0,//端口
        AliveType:1,//监听类别 0长链接 1短连接
        Listening:false, //正在监听
        Desc:'', //描述
        //转发列表
        Forwards:[
            {
                ID:1,   //编号
                ConnectionId:1, //目标 0位服务器
                SourceIp:'127.0.0.1', //应该叫host 因为短连接时可以填写域名
                TargetIp:'127.0.0.1', //目标节点本机ip,只能ip
                TargetPort:80,      //目标节点本机端口
                Desc:'', //描述
            }
        ]
    }
]
        `
    },
    {
        text: '添加修改监听',
        path: 'forward/addlisten',
        params: `
{
    ID:0,//编号 为0时添加，不为0修改
    Port:0,//端口
    AliveType:1,//监听类别 0长链接 1短连接
    Listening:false, //正在监听
    Desc:'', //描述
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '删除监听',
        path: 'forward/removelisten',
        params: `1 //监听id`,
        response: `true //true成功，false失败`
    },
    {
        text: '开启监听',
        path: 'forward/start',
        params: `1 //监听id`,
        response: `true //true成功，false失败`
    },
    {
        text: '停止监听',
        path: 'forward/stop',
        params: `1 //监听id`,
        response: `true //true成功，false失败`
    },
    {
        text: '添加修改转发',
        path: 'forward/addforward',
        params: `
{
    ListenID:0,//监听id
    Forward:{
        ID:0,//id 为0添加，不为0修改
        ConnectionId:0,//目标节点id，0为服务端
        SourceIp:'',//访问ip或域名
        TargetIp:'',//目标节点ip
        TargetPort:80,//目标节点端口
        Desc:'',//描述
    }
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '删除转发',
        path: 'forward/removeforward',
        params: `
{
    ListenID:0, // 监听id  
    ForwardID:0, //转发id
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '连通测试',
        path: 'forward/test',
        params: `
{
    Host:'', //ip 或域名，基本就是转发的源ip
    Port:'', //监听端口
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