using common.libs;
using common.proxy;
using System;
using System.Buffers.Binary;
using common.libs.extends;
using System.Net;

namespace common.socks5
{
    public interface ISocks5ProxyPlugin: IProxyPlugin
    {

    }

    public class Socks5ProxyPlugin : ISocks5ProxyPlugin
    {
        public virtual byte Id => config.Plugin;
        public virtual EnumProxyBufferSize BufferSize => config.BufferSize;
        public virtual ushort Port => (ushort)config.ListenPort;
        public virtual bool Enable => config.ListenEnable;

        private readonly Config config;
        private readonly IProxyServer proxyServer;
        private readonly ISocks5ConnectionProvider socks5ConnectionProvider;
        public Socks5ProxyPlugin(Config config, IProxyServer proxyServer, ISocks5ConnectionProvider socks5ConnectionProvider)
        {
            this.config = config;
            this.proxyServer = proxyServer;
            this.socks5ConnectionProvider = socks5ConnectionProvider;
        }

        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
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
            Socks5EnumStep socks5EnumStep = (Socks5EnumStep)info.Rsv;

            //request  auth 的 直接通过,跳过验证部分
            if (socks5EnumStep < Socks5EnumStep.Command)
            {
                switch (socks5EnumStep)
                {
                    case Socks5EnumStep.Request:
                        {
                            info.Response[0] = (byte)Socks5EnumAuthType.NoAuth;
                            info.Data = info.Response;
                        }
                        break;
                    case Socks5EnumStep.Auth:
                        {
                            info.Response[0] = (byte)Socks5EnumAuthState.Success;
                            info.Data = info.Response;
                        }
                        break;
                    default:
                        break;
                }
                proxyServer.InputData(info);
                return false;
            }

            socks5ConnectionProvider.Get(info);

            //command 的
            if (info.Step == EnumProxyStep.Command)
            {
                //udp中继的时候，有可能是 0.0.0.0:0 直接通过
                if (Socks5Parser.GetIsAnyAddress(info.Data))
                {
                    info.Response[0] = (byte)Socks5EnumResponseCommand.ConnecSuccess;
                    info.Data = info.Response;
                    proxyServer.InputData(info);
                    return false;
                }
                GetRemoteEndPoint(info);
            }
            else if (info.Step == EnumProxyStep.ForwardUdp)
            {
                info.Data = Socks5Parser.GetUdpData(info.Data);
                GetRemoteEndPoint(info);
            }

            return true;
        }

        public virtual bool ValidateAccess(ProxyInfo info)
        {
            return Enable;
        }

        public void HandleAnswerData(ProxyInfo info)
        {
            Socks5EnumStep socks5EnumStep = (Socks5EnumStep)info.Rsv;
            switch (socks5EnumStep)
            {
                case Socks5EnumStep.Request:
                    {
                        info.Data = new byte[] { 5, info.Data.Span[0] };
                        info.Rsv = (byte)Socks5EnumStep.Command;
                    }
                    break;
                case Socks5EnumStep.Auth:
                    {
                        info.Data = new byte[] { 5, info.Data.Span[0] };
                        info.Rsv = (byte)Socks5EnumStep.Command;
                    }
                    break;
                case Socks5EnumStep.Command:
                    {
                        Socks5EnumResponseCommand type = (Socks5EnumResponseCommand)info.Data.Span[0];
                        if (type == Socks5EnumResponseCommand.ConnecSuccess)
                        {
                            info.Data = Socks5Parser.MakeConnectResponse(new IPEndPoint(IPAddress.Any, Port), (byte)type);
                            info.Rsv = (byte)Socks5EnumStep.Forward;
                        }
                        else
                        {
                            info.Data = Helper.EmptyArray;
                        }
                    }
                    break;
                case Socks5EnumStep.Forward:
                    {
                        info.Rsv = (byte)Socks5EnumStep.Forward;
                    }
                    break;
                case Socks5EnumStep.ForwardUdp:
                    {
                        info.Data = Socks5Parser.MakeUdpResponse(new IPEndPoint(new IPAddress(info.TargetAddress.Span), info.TargetPort), info.Data);
                    }
                    break;
            }
        }


        private void GetRemoteEndPoint(ProxyInfo info)
        {
            //VERSION COMMAND RSV ATYPE  DST.ADDR  DST.PORT
            //去掉 VERSION COMMAND RSV
            var memory = info.Data.Slice(3);
            var span = memory.Span.Slice(3);
            info.AddressType = (EnumProxyAddressType)span[0];
            int index = 0;

            switch (info.AddressType)
            {
                case EnumProxyAddressType.IPV4:
                    {
                        info.TargetAddress = memory.Slice(1, 4);
                        index = 1 + 4;
                    }
                    break;
                case EnumProxyAddressType.Domain:
                    {
                        info.TargetAddress = memory.Slice(2, span[1]);
                        index = 2 + span[1];
                    }
                    break;
                case EnumProxyAddressType.IPV6:
                    {
                        info.TargetAddress = memory.Slice(1, 16);
                        index = 1 + 16;
                    }
                    break;
                default:
                    break;
            }

            info.TargetPort = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(index, 2));
        }
    }
}
