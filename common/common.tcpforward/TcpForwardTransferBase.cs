using common.libs;
using common.libs.extends;
using System.Text;

namespace common.tcpforward
{
    public class TcpForwardTransferBase
    {
        private readonly ITcpForwardServer tcpForwardServer;
        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        private readonly ITcpForwardTargetProvider tcpForwardTargetProvider;


        public TcpForwardTransferBase(ITcpForwardServer tcpForwardServer, TcpForwardMessengerSender tcpForwardMessengerSender, ITcpForwardTargetProvider tcpForwardTargetProvider)
        {
            this.tcpForwardServer = tcpForwardServer;
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.tcpForwardTargetProvider = tcpForwardTargetProvider;

            //A来了请求 ，转发到B，
            tcpForwardServer.OnRequest = OnRequest;
        }

        private bool OnRequest(TcpForwardInfo request)
        {
            if (request.Connection == null || request.Connection.Connected == false)
            {
                request.Connection = null;
                GetTarget(request);
            }

            if (request.Connection == null || request.Connection.Connected == false)
            {
                request.StateType = TcpForwardStateTypes.Fail;
                if (request.AliveType == TcpForwardAliveTypes.Web)
                {
                    request.Buffer = HttpParseHelper.BuildMessage("未选择转发对象，或者未与转发对象建立连接");
                }
                tcpForwardServer.Response(request);
                return true;
            }
            request.Connection.ReceiveBytes += request.Buffer.Length;
            return tcpForwardMessengerSender.SendRequest(request);
        }

        private void GetTarget(TcpForwardInfo request)
        {
            request.ForwardType = TcpForwardTypes.Forward;
            //缓存第一个包，等连接成功后发送
            request.Cache = request.Buffer;
            request.Buffer = Helper.EmptyArray;

            //短链接
            if (request.AliveType == TcpForwardAliveTypes.Web)
            {
                //http1.1代理
                if (HttpConnectMethodHelper.IsConnectMethod(request.Cache.Span))
                {
                    request.ForwardType = TcpForwardTypes.Proxy;
                    tcpForwardTargetProvider?.Get(request.SourcePort, request);
                    request.TargetEndpoint = HttpConnectMethodHelper.GetHost(request.Cache);
                    //request.Buffer = Helper.EmptyArray;
                }
                //正常的http请求
                else
                {
                    string domain = HttpParseHelper.GetHost(request.Cache.Span).GetString();
                    tcpForwardTargetProvider?.Get(domain, request);
                }
            }
            //长连接
            else
            {
                tcpForwardTargetProvider?.Get(request.SourcePort, request);
            }
        }
    }
}
