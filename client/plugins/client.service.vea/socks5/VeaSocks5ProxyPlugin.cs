using common.libs.extends;
using common.proxy;
using common.server;
using common.server.model;
using common.socks5;
using System.Buffers.Binary;
using System.Net;

namespace client.service.vea.socks5
{
    public interface IVeaSocks5ProxyPlugin : IProxyPlugin { }


    public class VeaSocks5ProxyPlugin : Socks5ProxyPlugin, IVeaSocks5ProxyPlugin
    {
        public override byte Id => config.Plugin;
        public override EnumBufferSize BufferSize => config.BufferSize;
        public override ushort Port => (ushort)config.ListenPort;
        public override bool Enable => config.ConnectEnable;

        private readonly Config config;
        private readonly IProxyServer proxyServer;
        private readonly VeaTransfer veaTransfer;
        private readonly IProxyMessengerSender proxyMessengerSender;

        public VeaSocks5ProxyPlugin(Config config, IProxyServer proxyServer
            , VeaTransfer veaTransfer, IProxyMessengerSender proxyMessengerSender) :
            base(null, proxyServer)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.veaTransfer = veaTransfer;
            this.proxyMessengerSender = proxyMessengerSender;
        }

        public override bool HandleRequestData(ProxyInfo info)
        {
            bool res = base.HandleRequestData(info);
            if (res == false) return res;

            Socks5EnumStep socks5EnumStep = (Socks5EnumStep)info.Rsv;

            if (socks5EnumStep == Socks5EnumStep.Command)
            {
                //组网支持IPV4
                if (info.AddressType != EnumProxyAddressType.IPV4)
                {
                    info.Response[0] = (byte)Socks5EnumResponseCommand.AddressNotAllow;
                    info.Data = info.Response;
                    proxyServer.InputData(info);
                    return false;
                }
                info.Connection = GetConnection(info);
            }
            else if (info.Step == EnumProxyStep.ForwardUdp)
            {
                //组网支持IPV4
                if (info.AddressType != EnumProxyAddressType.IPV4) return false;
                //组播数据包，直接分发
                if (info.TargetAddress.GetIsBroadcastAddress())
                {
                    foreach (var item in veaTransfer.IPList.Values)
                    {
                        info.Connection = item.Client.Connection;
                        proxyMessengerSender.Request(info);
                    }
                    return false;
                }
                info.Connection = GetConnection(info);
            }
            //组网支持IPV4
            if (info.AddressType != EnumProxyAddressType.IPV4)
            {
                return false;
            }

            return true;
        }

        public override bool ValidateAccess(ProxyInfo info)
        {
#if DEBUG
            return true;
#else
            return Enable;
#endif
        }

        private IConnection GetConnection(ProxyInfo info)
        {
            IPAddress target = new IPAddress(info.TargetAddress.Span);
            IConnection connection = null;
            if (veaTransfer.IPList.TryGetValue(target, out IPAddressCacheInfo cache))
            {
                connection = cache.Client.Connection;
            }
            else
            {
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(info.TargetAddress.Span);
                if (veaTransfer.LanIPList.TryGetValue(ip & 0xffffff00, out cache))
                {
                    connection = cache.Client.Connection;
                }
                if (veaTransfer.LanIPList.TryGetValue(ip & 0xffff0000, out cache))
                {
                    connection = cache.Client.Connection;
                }
                if (veaTransfer.LanIPList.TryGetValue(ip & 0xff000000, out cache))
                {
                    connection = cache.Client.Connection;
                }
            }

            return connection;
        }
    }
}
