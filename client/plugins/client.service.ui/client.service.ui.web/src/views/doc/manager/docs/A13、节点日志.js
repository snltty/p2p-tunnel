export default [
    {
        text: '配置文件',
        path: '',
        params: `//直接去使用配置文件接口修改配置即可`,
        response: `略`
    },
    {
        text: '日志列表',
        path: 'logger/list',
        params: `
{
    Page : 1,   //当前页
    PageSize :10,//页大小
    Type : 0, //日志类型 1debug 2info 3warning 4error
}
        `,
        response: `
{
    Page : 1,   //当前页
    PageSize :10,//页大小
    Count : 0, 
    Data:[
        {
            Type:0,       //类别
            Time:'',     //时间
            Content:0,     //内容
        }
    ]
}
        `
    },
    {
        text: '清空日志',
        path: 'logger/clear',
        params: `略`,
        response: `略`
    }
];