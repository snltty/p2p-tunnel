export default [
    {
        text: '先导说明',
        path: '协议为websocket',
        params: `
//公共请求结构，所有请求都按照这个结构，
{
    Path:'', //接口路径
    RequestId:0, //请求id
    Content:'', //参数内容
}

//举例
const ws = new WebSocket('ws://127.0.0.1:5412');
//发送字符串
ws.send(\`
{
    Path:'/signin/start', //接口路径
    RequestId:1, //请求id,服务端不做任何处理，会原样返回，用于前端识别请求
    Content:'', //参数内容
}
\`);
        `,
        response: `
//公共返回结构，所有返回都按照这个结构，
{

    Path:'', //请求路径
    RequestId:0, //请求id
    Code:0, //状态码  0success，1 not found，0xff error
    Content:'', //返回内容
}

//举例
ws.onmessage = (msg)=>{
    //这里得到的msg，就是这个返回结构 也是字符串
}
        `
    }
];