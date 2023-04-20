using common.libs.extends;
using common.proxy;
using common.server;
using common.server.model;
using common.socks5;
using System;
using System.Net;

namespace client.service.vea.socks5
{
    public interface IVeaSocks5ConnectionProvider : ISocks5ConnectionProvider { }
    public interface IVeaSocks5ProxyPlugin : IProxyPlugin { }

    public class VeaSocks5ConnectionProvider : IVeaSocks5ConnectionProvider
    {
        public void Get(ProxyInfo info)
        {
        }
    }


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
            , IVeaSocks5ConnectionProvider veaSocks5ConnectionProvider
            , VeaTransfer veaTransfer, IProxyMessengerSender proxyMessengerSender) :
            base(new common.socks5.Config(), proxyServer, veaSocks5ConnectionProvider)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.veaTransfer = veaTransfer;
            this.proxyMessengerSender = proxyMessengerSender;
        }

        public override bool HandleRequestData(ProxyInfo info)
        {
            if (info.Rsv == 0)
            {
                info.Rsv = (byte)Socks5EnumStep.Request;
            }

            Socks5EnumStep socks5EnumStep = (Socks5EnumStep)info.Rsv;

            if (socks5EnumStep == Socks5EnumStep.Command)
            {
                if (Socks5Parser.GetIsIPV4(info.Data) == false)
                {
                    info.Response[0] = (byte)Socks5EnumResponseCommand.AddressNotAllow;
                    info.Data = info.Response;
                    proxyServer.InputData(info);
                    return false;
                }
                if (Socks5Parser.GetIsIPV4AnyAddress(info.Data) == false)
                {
                    var targetEp = Socks5Parser.GetRemoteAddress(info.Data);
                    info.Connection = GetConnection(targetEp);
                }
            }
            else if (info.Step == EnumProxyStep.ForwardUdp)
            {
                if (Socks5Parser.GetIsIPV4(info.Data) == false) return false;
                if (Socks5Parser.GetIsIPV4AnyAddress(info.Data)) return false;
                //广播数据包
                if (Socks5Parser.GetIsBroadcastAddress(info.Data))
                {
                    base.HandleRequestData(info);
                    foreach (var item in veaTransfer.IPList.Values)
                    {
                        info.Connection = item.Client.Connection;
                        proxyMessengerSender.Request(info);
                    }
                    return false;
                }

                IPAddress address = Socks5Parser.GetRemoteAddress(info.Data);
                info.Connection = GetConnection(address);
            }
            return base.HandleRequestData(info);
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

    /// <summary>
    /// 组网消息
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum VeaSocks5MessengerIds : ushort
    {
        /// <summary>
        /// 最小
        /// </summary>
        Min = 1100,
        /// <summary>
        /// 更新ip
        /// </summary>
        Ip = 1101,
        /// <summary>
        /// 重装网卡
        /// </summary>
        Reset = 1102,
        /// <summary>
        /// 最大
        /// </summary>
        Max = 1199,
    }
}
