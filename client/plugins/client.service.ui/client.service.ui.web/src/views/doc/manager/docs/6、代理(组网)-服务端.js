export default [
    {
        text: '本组网络信息',
        path: 'servervea/list',
        params: `略 //获取的是本分组的网络信息，每个分组都有一个网络`,
        response: `
{
    IP:0, //大端uint 网段 
    Used:[0,0,0,0], //已使用ip缓存，方便查询 4个ulong，32字节，256位，每一位表示一个ip，0未使用，1已使用
    //已分配列表，有哪些节点使用了什么ip
    Assigned:{
        //连接id
        1:{
            IP:0,   //大端uint 表示这个节点的ip
            OnLine:true, //节点是否在线
            LastTime:'2023-06-30 16:37:37', //最后使用时间
            Name:'', //节点名
        }
    }
}
        `
    },
    {
        text: '修改节点的ip',
        path: 'servervea/modifyip',
        params: `
{
    ConnectionId:1, //连接id   
    IP:1, //ip，一个字节，类似于 192.168.54.1 最后的那一位1    
}
        `,
        response: `true //true成功 false失败`
    },
    {
        text: '删除节点的ip',
        path: 'servervea/deleteip',
        params: `1 //连接id`,
        response: `true //true成功 false失败`
    },
];