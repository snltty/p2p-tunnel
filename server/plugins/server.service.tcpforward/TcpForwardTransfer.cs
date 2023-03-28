using common.libs;
using common.tcpforward;
using server.messengers.singnin;
using System.Linq;

namespace server.service.tcpforward
{
    /// <summary>
    /// tcp转发中转和入口
    /// </summary>
    public sealed class TcpForwardTransfer : TcpForwardTransferBase
    {
        public TcpForwardTransfer(

            common.tcpforward.Config config, IClientSignInCaching clientSignInCaching,
            ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching,
            ITcpForwardServer tcpForwardServer,
            TcpForwardMessengerSender tcpForwardMessengerSender,
            ITcpForwardTargetProvider tcpForwardTargetProvider) : base(tcpForwardServer, tcpForwardMessengerSender, tcpForwardTargetProvider)
        {
            if (config.ConnectEnable)
            {
                tcpForwardServer.Init(config.NumConnections, config.BufferSize);

                //离线删除其监听
                clientSignInCaching.OnOffline += (client) =>
                {
                    var keys = tcpForwardTargetCaching.Remove(client.Name);
                    if (keys.Any())
                    {
                        foreach (var item in keys)
                        {
                            tcpForwardServer.Stop(item);
                        }
                    }
                };

                Logger.Instance.Info("TCP转发服务已启动...");
                //转发监听
                foreach (ushort port in config.WebListens)
                {
                    tcpForwardServer.Start(port, TcpForwardAliveTypes.Web);
                    Logger.Instance.Warning($"TCP转发监听:{port}");
                }
            }
        }
    }
}