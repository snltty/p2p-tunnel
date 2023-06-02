using common.libs;
using common.proxy;
using System.Net;
using common.server.model;
using common.libs.extends;
using System;
using common.server;

namespace common.socks5
{
    public interface ISocks5ProxyPlugin : IProxyPlugin
    {
    }
    public class Socks5ProxyPlugin : ISocks5ProxyPlugin
    {
        public virtual byte Id => config.Plugin;
        public virtual bool ConnectEnable => config.ListenEnable;
        public virtual EnumBufferSize BufferSize => config.BufferSize;
        public virtual IPAddress BroadcastBind => IPAddress.Any;
        public virtual HttpHeaderDynamicInfo Headers { get; set; }

        public virtual uint Access => 0b00000000_00000000_00000000_00010000;
        public virtual string Name => "socks5";
        public virtual ushort Port => (ushort)config.ListenPort;



        private readonly Config config;
        private readonly IProxyServer proxyServer;
        public Socks5ProxyPlugin(Config config, IProxyServer proxyServer)
        {
            this.config = config;
            this.proxyServer = proxyServer;
        }

        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            if (info.Rsv == 0)
            {
                info.Rsv = (byte)Socks5EnumStep.Request;
            }
            return (Socks5EnumStep)info.Rsv switch
            {
                Socks5EnumStep.Request => Socks5Parser.ValidateRequestData(info.Data),
                Socks5EnumStep.Command => Socks5Parser.ValidateCommandData(info.Data),
                Socks5EnumStep.Auth => Socks5Parser.ValidateAuthData(info.Data, Socks5EnumAuthType.Password),
                Socks5EnumStep.Forward => EnumProxyValidateDataResult.Equal,
                Socks5EnumStep.ForwardUdp => EnumProxyValidateDataResult.Equal,
                _ => EnumProxyValidateDataResult.Equal
            };
        }

        public virtual bool HandleRequestData(ProxyInfo info)
        {
            if (info.Rsv == 0)
            {
                info.Rsv = (byte)Socks5EnumStep.Request;
            }

            Socks5EnumStep socks5EnumStep = (Socks5EnumStep)info.Rsv;
            //request  auth 的 直接通过,跳过验证部分
            if (socks5EnumStep < Socks5EnumStep.Command)
            {
                //Socks5EnumAuthType.NoAuth不验证  Socks5EnumAuthState.Success验证成功 都是 0x00
                info.Response[0] = 0x00;
                info.Data = info.Response;
                info.Rsv++;
                proxyServer.InputData(info);
                return false;
            }
            //command 的
            if (info.Step == EnumProxyStep.Command)
            {
                //解析出目标地址
                GetRemoteEndPoint(info);
                //udp中继的时候，有可能是 0.0.0.0:0 直接通过
                if (info.TargetAddress.GetIsAnyAddress())
                {
                    info.Response[0] = (byte)Socks5EnumResponseCommand.ConnecSuccess;
                    info.Data = info.Response;
                    proxyServer.InputData(info);
                    return false;
                }

                //将socks5的command转化未通用command
                info.Command = (EnumProxyCommand)info.Data.Span[1];
                //此时的负载是socks5的command包，直接去掉
                info.Data = Helper.EmptyArray;
            }
            else if (info.Step == EnumProxyStep.ForwardUdp)
            {
                //解析出目标地址
                GetRemoteEndPoint(info);
                //解析出udp包的数据部分
                info.Data = Socks5Parser.GetUdpData(info.Data);
            }
            if (info.TargetAddress.GetIsAnyAddress()) return false;

            return true;
        }

        public bool HandleAnswerData(ProxyInfo info)
        {
            Socks5EnumStep socks5EnumStep = (Socks5EnumStep)info.Rsv;

            //request auth 步骤的，只需回复一个字节的状态码
            if (socks5EnumStep < Socks5EnumStep.Command)
            {
                info.Data = new byte[] { 5, info.Data.Span[0] };
                info.Rsv = (byte)Socks5EnumStep.Command;
                return true;
            }

            switch (info.Step)
            {
                case EnumProxyStep.Command:
                    {
                        //command的，需要区分成功和失败，成功则回复指定数据，失败则关闭连接
                        Socks5EnumResponseCommand type = (Socks5EnumResponseCommand)info.Data.Span[0];
                        info.Data = Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, Port), (byte)type);
                        //走到转发步骤
                        info.Rsv = (byte)Socks5EnumStep.Forward;
                        info.Step = EnumProxyStep.ForwardTcp;
                    }
                    break;
                case EnumProxyStep.ForwardTcp:
                    {
                        info.Rsv = (byte)Socks5EnumStep.Forward;
                        info.Step = EnumProxyStep.ForwardTcp;
                    }
                    break;
                case EnumProxyStep.ForwardUdp:
                    {
                        //组装udp包
                        info.Data = Socks5Parser.MakeUdpResponse(new IPEndPoint(new IPAddress(info.TargetAddress.Span), info.TargetPort), info.Data);
                        info.Rsv = (byte)Socks5EnumStep.ForwardUdp;
                        info.Step = EnumProxyStep.ForwardUdp;
                    }
                    break;
            }
            return true;
        }

        protected void GetRemoteEndPoint(ProxyInfo info)
        {
            info.TargetAddress = Socks5Parser.GetRemoteEndPoint(info.Data, out Socks5EnumAddressType addressType, out ushort port);
            info.AddressType = (EnumProxyAddressType)addressType;
            info.TargetPort = port;
        }
    }
}
