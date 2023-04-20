using common.libs;
using common.libs.extends;
using common.proxy;
using common.server.model;
using System;
using System.Linq;
using System.Text;

namespace common.forward
{
    public interface IForwardProxyPlugin : IProxyPlugin
    {
        public Action<ushort> OnStarted { get; set; }
        public Action<ushort> OnStoped { get; set; }
    }

    public sealed class ForwardProxyPlugin : IForwardProxyPlugin
    {
        public byte Id => config.Plugin;
        public EnumBufferSize BufferSize => config.BufferSize;
        public Action<ushort> OnStarted { get; set; } = (port) => { };
        public Action<ushort> OnStoped { get; set; } = (port) => { };


        private readonly Config config;
        private readonly IProxyServer proxyServer;
        private readonly IForwardTargetProvider forwardTargetProvider;
        private readonly IForwardUdpTargetProvider forwardUdpTargetProvider;
        public ForwardProxyPlugin(Config config, IProxyServer proxyServer, IForwardTargetProvider forwardTargetProvider, IForwardUdpTargetProvider forwardUdpTargetProvider)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.forwardTargetProvider = forwardTargetProvider;
            this.forwardUdpTargetProvider = forwardUdpTargetProvider;
        }

        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return EnumProxyValidateDataResult.Equal;
        }

        public bool HandleRequestData(ProxyInfo info)
        {
            if (info.Connection == null || info.Connection.Connected == false)
            {
                info.Connection = null;
                GetConnection(info);
            }

            if (info.Connection == null || info.Connection.Connected == false)
            {
                info.Data = Helper.EmptyArray;
                proxyServer.InputData(info);
                return true;
            }

            info.AddressType =  EnumProxyAddressType.IPV4;

            return true;
        }
        public void HandleAnswerData(ProxyInfo info) { }
        public bool ValidateAccess(ProxyInfo info)
        {
            if (config.PortWhiteList.Length > 0 && config.PortWhiteList.Contains(info.TargetPort) == false)
            {
                return false;
            }
            if (config.PortBlackList.Length > 0 && config.PortBlackList.Contains(info.TargetPort))
            {
                return false;
            }
            return config.ConnectEnable;
        }


        public void Started(ushort port)
        {
            OnStarted(port);
        }
        public void Stoped(ushort port)
        {
            OnStoped(port);
        }

        private void GetConnection(ProxyInfo info)
        {
            if (info.Command == EnumProxyCommand.UdpAssociate || forwardUdpTargetProvider.Contains(info.ListenPort))
            {
                forwardUdpTargetProvider?.Get(info.ListenPort, info);
            }
            else
            {
                int portStart = 0;
                string host = HttpParser.GetHost(info.Data,ref portStart).GetString();
                forwardTargetProvider?.Get(host, info);
            }
        }
    }
}
