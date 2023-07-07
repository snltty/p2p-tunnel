export default [
    {
        text: '配置文件',
        path: 'serverproxy/get',
        params: `略`,
        response: `
{
    Firewall:[
        {
            ID:0,       //ID
            PluginId:0, //作用代理功能，作用与哪些代理功能，0xff所有
            Protocol:0, //作用于协议，1tcp 2udp 3tcp+udp
            Type:0,     //类别 0允许 1阻止
            Port:'',     //端口，英文逗号间隔表示多个,/表示范围(80,443,5000/6000)
            IP:['',''],  //ip数组，支持掩码
            Remark:'',  //备注 
        }
    ]
}
        `
    },
    {
        text: '添加修改',
        path: 'serverproxy/add',
        params: `
{
    ID:0,       //ID 0添加 不为0修改
    PluginId:0, //作用代理功能，作用与哪些代理功能，0xff所有
    Protocol:0, //作用于协议，1tcp 2udp 3tcp+udp
    Type:0,     //类别 0允许 1阻止
    Port:'',     //端口，英文逗号间隔表示多个,/表示范围(80,443,5000/6000)
    IP:['',''],  //ip数组，支持掩码
    Remark:'',  //备注 
}
        `,
        response: `true //true成功，false失败`
    },
    {
        text: '删除一项',
        path: 'serverproxy/remove',
        params: `id`,
        response: `true //true成功，false失败`
    }
];