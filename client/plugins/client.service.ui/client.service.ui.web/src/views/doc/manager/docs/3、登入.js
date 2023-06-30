export default [
    {
        text: '获取配置',
        path: 'signin/info',
        params: `略`,
        response: `
{
    //节点配置信息
    ClientConfig:{
        GroupId:'', //分组编号
        Name:'', //节点名
        Args:'', //动态参数
        AutoReg:false, //自动登入
        TcpBufferSize:7, //tcp连接缓冲区大小 1-10 分别表示 1-1024KB
        Encode:false, //是否加密
        EncodePassword:'', //加密秘钥，必须32位
        TimeoutDelay:20000, //掉线超时ms
        UsePunchHole:true, //自动打洞
        UseUdp:true, //使用udp
        UseTcp:true, //使用tcp
        UseRelay:true, //作为中继节点
        AutoRelay:true, //自动中继
        UdpUploadSpeedLimit:1048576, //udp限速，byte
        Services:[], //只显示哪些ui
        ConnectId:1, //连接id
        TTL:1, //打洞附加TTL
    },
    //服务器配置信息
    ServerConfig:{
        Ip:'127.0.0.1', //服务器ip
        UdpPort:5410, //服务器udp端口
        TcpPort:5410, //服务器tcp端口
        Encode:false, //服务器通信加密
        EncodePassword:'', //加密秘钥，必须32位
        //服务器备选列表
        Items:[       
            { Img="zg", Ip = "127.0.0.1", TcpPort = 5410, UdpPort = 5410, Name = "本地" }
        ],
    },
    //本节点信息
    LocalInfo:{
        RouteLevel:1,//外网距离
        Port:0,//本机端口
        LocalIp:'127.0.0.1',//本机ip
        IsConnecting:true, //正在登入服务器
        Connected:true, //已登入服务器
    },
    //外网信息
    RemoteInfo:{
        Ip:'127.0.0.1', //外网ip
        ConnectId:1, //连接id
        Access:0, //服务器权限
    },
}
        `
    },
    {
        text: '更新配置',
        path: 'signin/config',
        params: `略`,
        response: `
{
    //节点配置信息，传全部字段
    ClientConfig:{},
    //服务器节点信息，传全部字段
    ServerConfig:{},
}
        `
    },
    {
        text: '与服务器连接',
        path: 'signin/start',
        params: `略`,
        response: `true //true成功，false失败`
    },
    {
        text: '与服务器断开',
        path: 'signin/exit',
        params: `略`,
        response: `true //true成功，false失败`
    },
    {
        text: '服务器延迟',
        path: 'signin/ping',
        params: `[] //服务器ip列表`,
        response: `[] //延迟列表，-1超时`
    },
]