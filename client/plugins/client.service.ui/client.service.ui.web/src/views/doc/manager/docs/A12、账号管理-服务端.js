export default [
    {
        text: '配置文件',
        path: '',
        params: `//直接去使用配置文件接口修改配置即可`,
        response: `略`
    },
    {
        text: '账号列表',
        path: 'serverusers/list',
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
        text: '添加账号',
        path: 'serverusers/add',
        params: `
{
    ID:0,           //ID  0添加 不为0修改
    Account:'',     //账号
    Password:'',    //密码
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
        `,
        response: `为空成功，不为空则为错误信息`
    },
    {
        text: '修改密码',
        path: 'serverusers/password',
        params: `
{
    ID:0,           //ID
    Password:'',     //密码
}
        `,
        response: `为空成功，不为空则为错误信息`
    },
    {
        text: '删除账号',
        path: 'serverusers/remove',
        params: `ID //账号ID`,
        response: `为空成功，不为空则为错误信息`
    },
    {
        text: '账号信息',
        path: 'serverusers/info',
        params: `略 //获取自己登入的账号信息`,
        response: `
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
        `
    },
    {
        text: '修改密码',
        path: 'serverusers/passwordSelf',
        params: `密码 //修改自己的账号密码`,
        response: `为空成功，不为空则为错误信息`
    },
];