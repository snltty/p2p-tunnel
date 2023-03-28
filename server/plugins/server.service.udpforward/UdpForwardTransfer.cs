using common.libs;
using common.udpforward;
using server.messengers.singnin;
using System.Linq;

namespace server.service.udpforward
{
    public sealed class UdpForwardTransfer : UdpForwardTransferBase
    {
        public UdpForwardTransfer(

            common.udpforward.Config config, IClientSignInCaching clientSignInCaching,
            IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching,
            IUdpForwardServer udpForwardServer,
            UdpForwardMessengerSender udpForwardMessengerSender,
            IUdpForwardTargetProvider udpForwardTargetProvider) : base(udpForwardServer, udpForwardMessengerSender, udpForwardTargetProvider)
        {
            if (config.ConnectEnable)
            {
                //离线删除其监听
                clientSignInCaching.OnOffline += (client) =>
                {
                    var keys = udpForwardTargetCaching.Remove(client.Name);
                    if (keys.Any())
                    {
                        foreach (var item in keys)
                        {
                            udpForwardServer.Stop(item);
                        }
                    }
                };

                Logger.Instance.Info("UDP转发服务已启动...");
            }
        }
    }
}