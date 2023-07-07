export default [
    {
        text: '配置信息',
        path: '',
        params: `//直接去使用配置文件接口修改配置即可`,
        response: `略`
    },
    {
        text: '转发列表',
        path: 'serverforward/list',
        params: `略`,
        response: `
[
    {
        AliveType:0,//0长链接 1短连接
        Domain:'',//域名，短连接时有，长链接可以忽略此项
        ServerPort:0,//在服务器监听的端口
        LocalIp:'127.0.0.1',//本机ip
        LocalPort:80,//本机端口
        Desc:'',//描述
        Listening:true,//正在监听
    }
]
        `
    },
    {
        text: '添加转发',
        path: 'serverforward/add',
        params: `
{
    AliveType:0,//0长链接 1短连接
    Domain:'',//域名，短连接时有，长链接可以忽略此项
    ServerPort:0,//在服务器监听的端口
    LocalIp:'127.0.0.1',//本机ip
    LocalPort:80,//本机端口
    Desc:'',//描述
    Listening:true,//正在监听
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '删除转发',
        path: 'serverforward/remove',
        params: `
{
    AliveType:0,//0长链接 1短连接
    Domain:'',//域名，短连接时有，长链接可以忽略此项
    ServerPort:0,//在服务器监听的端口
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '开启转发',
        path: 'serverforward/start',
        params: `
{
    AliveType:0,//0长链接 1短连接
    Domain:'',//域名，短连接时有，长链接可以忽略此项
    ServerPort:0,//在服务器监听的端口
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '停止转发',
        path: 'serverforward/stop',
        params: `
{
    AliveType:0,//0长链接 1短连接
    Domain:'',//域名，短连接时有，长链接可以忽略此项
    ServerPort:0,//在服务器监听的端口
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '连通测试',
        path: '无此接口，可以使用A11的连通测试接口',
        params: `略`,
        response: `略`
    },
];