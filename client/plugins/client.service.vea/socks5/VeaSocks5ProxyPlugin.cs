using common.libs;
using common.libs.extends;
using common.proxy;
using common.server;
using common.server.model;
using common.socks5;
using System;
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
            }

            info.Connection = GetConnection(new IPAddress(info.TargetAddress.Span));
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

        byte[] ipBytes = new byte[4];
        private IConnection GetConnection(IPAddress target)
        {
            IConnection connection = null;
            if (veaTransfer.IPList.TryGetValue(target, out IPAddressCacheInfo cache))
            {
                connection = cache.Client.Connection;
            }
            else
            {
                if (target.IsLan())
                {
                    target.TryWriteBytes(ipBytes, out int len);
                    int ip = ipBytes.AsSpan().ToInt32();
                    if (veaTransfer.LanIPList.TryGetValue(ip & 0xffffff, out cache))
                    {
                        connection = cache.Client.Connection;
                    }
                    if (veaTransfer.LanIPList.TryGetValue(ip & 0xffff, out cache))
                    {
                        connection = cache.Client.Connection;
                    }
                    if (veaTransfer.LanIPList.TryGetValue(ip & 0xff, out cache))
                    {
                        connection = cache.Client.Connection;
                    }
                }
            }

            return connection;
        }
    }
}
