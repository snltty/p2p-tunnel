export default [
    {
        text: '账号列表',
        path: 'users/list',
        params: `
{
    Page : 1,   //当前页
    PageSize :10,//页大小
    Account :'', //按账号搜索
    Sort : 0, //排序byte 0b11111111 最高位0asc 1desc，低7位 1ID 2AddTime 4EndTime 8NetFlow 16SignLimit
}
        `,
        response: `
{
    Page : 1,   //当前页
    PageSize :10,//页大小
    Count : 0, 
    Data:[
        {
            ID:0,           //ID
            Account:'',     //账号
            Access:0,       //权限
            SignLimit:0,    //登入数量限制
            SignLimitType:0,//登入数量限制类型 0不限制 1限制
            SignCount:0,    //已登入数
            NetFlow:0,      //流量限制
            NetFlowType:0,  //流量限制类型 0不限制 1限制
            SentBytes:0,    //已发送流量
            AddTime:'2023-06-30 21:33:33',  //添加时间
            EndTime:'2023-06-30 21:33:33',  //结束时间
        }
    ]
}
        `
    },
    {
        text: '更新权限',
        path: 'users/update',
        params: `
{
    ID : 1,   //ID
    Access :0,//权限值
}
        `,
        response: `true //true成功，false失败`
    },
];