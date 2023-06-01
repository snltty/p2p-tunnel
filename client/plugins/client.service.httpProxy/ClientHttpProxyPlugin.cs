using client.messengers.clients;
using client.messengers.singnin;
using common.httpProxy;
using common.libs;
using common.libs.extends;
using common.proxy;
using System;
using System.Net;

namespace client.service.httpProxy
{
    public interface IClientHttpProxyPlugin : IProxyPlugin
    {
    }

    public class ClientHttpProxyPlugin : HttpProxyPlugin, IClientHttpProxyPlugin
    {
        private readonly common.httpProxy.Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IProxyServer proxyServer;
        public ClientHttpProxyPlugin(common.httpProxy.Config config, IClientInfoCaching clientInfoCaching, SignInStateInfo signInStateInfo, IProxyServer proxyServer) : base(config)
        {
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.signInStateInfo = signInStateInfo;
            this.proxyServer = proxyServer;
        }

        public override bool HandleRequestData(ProxyInfo info)
        {
            if (info.Step == EnumProxyStep.Command)
            {
                GetTargetEndPoint(info);
                info.Data = Helper.EmptyArray;
            }
            
            if (info.Connection == null || info.Connection.Connected == false)
            {
                if (config.TargetConnectionId == 0)
                {
                    info.Connection = signInStateInfo.Connection;
                }
                else
                {
                    if (clientInfoCaching.Get(config.TargetConnectionId, out ClientInfo client))
                    {
                        info.Connection = client.Connection;
                    }
                }
            }

            if (info.Connection == null || info.Connection.Connected == false || info.TargetAddress.Length == 0)
            {
                proxyServer.InputData(info);
                return false;
            }
            return true;
        }

        private void GetTargetEndPoint(ProxyInfo info)
        {
            info.TargetPort = 80;
            info.TargetAddress = Helper.EmptyArray;

            int portStart = 0;
            Memory<byte> memory = HttpParser.GetHost(info.Data, ref portStart);
            Memory<byte> hostMemory = memory;

            //带端口号
            if (portStart > 0)
            {
                hostMemory = memory.Slice(0, portStart);
                if (ushort.TryParse(memory.Slice(portStart + 1).GetString(), out ushort port))
                {
                    info.TargetPort = port;
                }
            }
            if(hostMemory.Length > 0)
            {
                //是ip
                if (IPAddress.TryParse(hostMemory.GetString(), out IPAddress ip))
                {
                    info.TargetAddress = ip.GetAddressBytes();
                    info.AddressType = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? EnumProxyAddressType.IPV4 : EnumProxyAddressType.IPV6;
                }
                else
                {
                    info.AddressType = EnumProxyAddressType.Domain;
                    info.TargetAddress = hostMemory;
                }
            }
        }
    }

}
