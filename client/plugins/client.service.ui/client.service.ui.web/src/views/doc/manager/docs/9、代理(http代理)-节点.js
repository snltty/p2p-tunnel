export default [
    {
        text: '配置信息',
        path: '',
        params: `//直接去使用配置文件接口修改配置即可`,
        response: `略`
    },
    {
        text: '运行',
        path: 'httpproxy/run',
        params: `略 //配置文件里有个ListenEnable，true时此接口开始监听，false时此接口停止监听`,
        response: `true //true成功，false失败`
    },
    {
        text: '连通测试',
        path: 'httpproxy/test',
        params: `略 //会去尝试连接 www.baidu.com:443`,
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