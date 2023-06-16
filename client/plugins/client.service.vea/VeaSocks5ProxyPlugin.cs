using client.messengers.clients;
using client.messengers.singnin;
using common.libs.extends;
using common.proxy;
using common.server.model;
using common.socks5;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;

namespace client.service.vea
{
    public interface IVeaSocks5ProxyPlugin : IProxyPlugin { }


    public class VeaSocks5ProxyPlugin : Socks5ProxyPlugin, IVeaSocks5ProxyPlugin
    {
        public override byte Id => config.Plugin;
        public override bool ConnectEnable => config.ConnectEnable;
        public override EnumBufferSize BufferSize => config.BufferSize;
        public override IPAddress BroadcastBind => config.BroadcastBind;
        public override HttpHeaderCacheInfo Headers { get; set; }
        public override Memory<byte> HeadersBytes { get; set; }

        public override uint Access => 0b00000000_00000000_00000000_01000000;
        public override string Name => "vea";

        public override ushort Port => (ushort)config.ListenPort;

        private readonly Config config;
        private readonly IProxyServer proxyServer;
        private readonly VeaTransfer veaTransfer;
        private readonly IProxyMessengerSender proxyMessengerSender;

        public VeaSocks5ProxyPlugin(Config config, client.Config config1, IProxyServer proxyServer
            , VeaTransfer veaTransfer, IProxyMessengerSender proxyMessengerSender, SignInStateInfo signInStateInfo) : base(null, proxyServer)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.veaTransfer = veaTransfer;
            this.proxyMessengerSender = proxyMessengerSender;

            signInStateInfo.OnChange += (bool state) =>
            {
                Headers = new HttpHeaderCacheInfo
                {
                    Addr = signInStateInfo.RemoteInfo.Ip,
                    Name = config1.Client.Name,
                    Proxy = Name
                };
            };
        }

        public override bool HandleRequestData(ProxyInfo info)
        {
            bool res = base.HandleRequestData(info);
            if (res == false) return res;

            Socks5EnumStep socks5EnumStep = (Socks5EnumStep)info.Rsv;

            if (info.Step == EnumProxyStep.Command && socks5EnumStep == Socks5EnumStep.Command)
            {
                //组网支持IPV4
                if (info.AddressType != EnumProxyAddressType.IPV4)
                {
                    info.CommandStatus = (EnumProxyCommandStatus)Socks5EnumResponseCommand.AddressNotAllow;
                    info.CommandStatusMsg = EnumProxyCommandStatusMsg.Address;
                    proxyServer.InputData(info);
                    return false;
                }
                GetConnection(info);
                if (info.Connection == null || info.Connection.Connected == false)
                {
                    info.CommandStatus = (EnumProxyCommandStatus)Socks5EnumResponseCommand.NetworkError;
                    info.CommandStatusMsg = EnumProxyCommandStatusMsg.Connection;
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
                    //没开启组播
                    if (config.BroadcastEnable == false) return false;
                    //组播ip不包含在允许列表里
                    if (config.VeaBroadcastList.Length > 0 && config.VeaBroadcastList.Contains(BinaryPrimitives.ReadUInt32BigEndian(info.TargetAddress.Span)) == false)
                    {
                        return false;
                    }

                    foreach (var item in veaTransfer.IPList.Values)
                    {
                        info.Connection = item.Client.Connection;
                        proxyMessengerSender.Request(info, unconnectedMessage: false);
                    }
                    return false;
                }
                GetConnection(info);
            }
            //组网支持IPV4
            if (info.AddressType != EnumProxyAddressType.IPV4)
            {
                return false;
            }

            return true;
        }

        private void GetConnection(ProxyInfo info)
        {
            if (veaTransfer.IPList.TryGetValue(BinaryPrimitives.ReadUInt32BigEndian(info.TargetAddress.Span), out IPAddressCacheInfo cache))
            {
                info.Connection = cache.Client.Connection;
            }
            else
            {
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(info.TargetAddress.Span);
                for (int i = 32; i >= 16; i--)
                {
                    if (veaTransfer.LanIPList.TryGetValue(ip & 0xffffffff << (32 - i), out cache))
                    {
                        info.Connection = cache.Client.Connection;
                        break;
                    }
                }
            }
        }
    }
}
