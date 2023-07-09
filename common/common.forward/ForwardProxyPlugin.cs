using common.libs;
using common.libs.extends;
using common.proxy;
using common.server.model;
using System;
using System.Net;
using System.Text;

namespace common.forward
{
    public interface IForwardProxyPlugin : IProxyPlugin
    {
        public Action<ushort> OnStarted { get; set; }
        public Action<ushort> OnStoped { get; set; }
    }

    public class ForwardProxyPlugin : IForwardProxyPlugin
    {
        public byte Id => config.Plugin;
        public bool ConnectEnable => config.ConnectEnable;
        public EnumBufferSize BufferSize => config.BufferSize;
        public IPAddress BroadcastBind => IPAddress.Any;
        public virtual HttpHeaderCacheInfo Headers { get; set; }
        public virtual Memory<byte> HeadersBytes { get; set; }

        public virtual uint Access => 0b00000000_00000000_00000000_00001000;
        public virtual string Name => "port-forward";

        public Action<ushort> OnStarted { get; set; } = (port) => { };
        public Action<ushort> OnStoped { get; set; } = (port) => { };

        private readonly Config config;
        private readonly IProxyServer proxyServer;
        private readonly IForwardTargetProvider forwardTargetProvider;
        public ForwardProxyPlugin(Config config, IProxyServer proxyServer, IForwardTargetProvider forwardTargetProvider)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.forwardTargetProvider = forwardTargetProvider;
        }

        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return EnumProxyValidateDataResult.Equal;
        }

        public virtual bool HandleRequestData(ProxyInfo info)
        {
            bool isMagicData = info.Step == EnumProxyStep.Command && HttpParser.GetIsCustomConnect(info.Data);
            if (info.Connection == null || info.Connection.Connected == false)
            {
                info.Connection = null;
                GetConnection(info);
            }
            if (isMagicData)
            {
                info.Data = ProxyHelper.MagicData;
            }

            if (info.Connection == null || info.Connection.Connected == false)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error($"【{info.ProxyPlugin.Name}】{info.RequestId} connection fail");
                if (isMagicData == false)
                    info.Data = Helper.EmptyArray;
                info.CommandStatusMsg = EnumProxyCommandStatusMsg.Connection;
                proxyServer.InputData(info);
                return false;
            }

            info.AddressType = EnumProxyAddressType.IPV4;
            return true;
        }
        public bool HandleAnswerData(ProxyInfo info)
        {
            if (info.Step == EnumProxyStep.Command)
            {
                info.Step = EnumProxyStep.ForwardTcp;
                return false;
            }
            return true;
        }

        public void Started(ushort port)
        {
            OnStarted(port);
        }
        public void Stoped(ushort port)
        {
            OnStoped(port);
        }

        //80443
        private Memory<byte> port = new byte[] { 56, 48, 52, 52, 51 };
        private void GetConnection(ProxyInfo info)
        {
            ForwardAliveTypes aliveTypes = (ForwardAliveTypes)info.Rsv;
            if (aliveTypes == ForwardAliveTypes.Tunnel)
            {
                forwardTargetProvider.Get(info.ListenPort, info);
            }
            else
            {
                int portStart = 0;
                Memory<byte> hostBytes = HttpParser.GetHost(info.Data, ref portStart);
                if (portStart > 0)
                {
                    //80 443
                    Span<byte> hostPort = hostBytes.Slice(portStart + 1).Span;
                    if (hostPort.SequenceEqual(port.Span.Slice(0, 2)) || hostPort.SequenceEqual(port.Span.Slice(2)))
                    {
                        hostBytes = hostBytes.Slice(0, portStart);
                    }
                }
                if (hostBytes.Length > 0)
                {
                    forwardTargetProvider.Get(hostBytes.GetString(), info);
                }
            }
        }


    }
}
