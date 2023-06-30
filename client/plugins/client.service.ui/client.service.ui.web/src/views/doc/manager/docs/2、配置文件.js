export default [
    {
        text: '配置文件列表',
        path: 'Configure/Configures',
        params: `略`,
        response: `
//所有功能的配置文件都在这里
[
    {
        Name:'', //名称
        ClassName:'', //配置文件名,获取配置文件内容时，传这个
        Author:'', //作者
        Desc:'', //描述
        Enable:true, //是否启用
    }
]
        `
    },
    {
        text: '获取配置文件内容',
        path: 'Configure/Configure',
        params: `ServerConfigure //配置文件名`,
        response: `具体配置文件内容`
    },
    {
        text: '保存配置文件内容',
        path: 'Configure/Save',
        params: `
{
    ClassName:'', //配置文件名
    Content:'', //内容
}
        `,
        response: `true //true成功 false失败`
    }
];