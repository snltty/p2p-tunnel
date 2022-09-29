using common.libs;
using common.libs.extends;
using common.server;
using common.udpforward;
using server.messengers.register;
using System;
using System.Linq;

namespace server.service.udpforward
{
    public class UdpForwardMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly common.udpforward.Config config;
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly UdpForwardMessengerSender tcpForwardMessengerSender;
        private readonly IUdpForwardServer tcpForwardServer;
        private readonly IUdpForwardValidator udpForwardValidator;

        public UdpForwardMessenger(IClientRegisterCaching clientRegisterCache, common.udpforward.Config config, IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> tcpForwardTargetCaching, UdpForwardMessengerSender tcpForwardMessengerSender, IUdpForwardServer tcpForwardServer, IUdpForwardValidator udpForwardValidator)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.config = config;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.tcpForwardServer = tcpForwardServer;
            this.udpForwardValidator = udpForwardValidator; 
        }

        public void Request(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            tcpForwardMessengerSender.OnRequest(data);
        }

        public void Response(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            tcpForwardMessengerSender.OnResponse(data);
        }

        public byte[] GetPorts(IConnection connection)
        {
            return new int[] {
                config.TunnelListenRange.Min,
                    config.TunnelListenRange.Max
                }.ToBytes();
        }

        public byte[] UnRegister(IConnection connection)
        {
            if (udpForwardValidator.Validate(connection) == false)
            {
                return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.DISABLED }.ToBytes();
            }

            try
            {
                int port = connection.ReceiveRequestWrap.Memory.Span.ToUInt16();

                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    UdpForwardTargetCacheInfo cache = tcpForwardTargetCaching.Get(port);
                    if(cache != null && cache.Name == source.Name)
                    {
                        tcpForwardTargetCaching.Remove(port);
                        tcpForwardServer.Stop(port);
                    }
                }
                return new UdpForwardRegisterResult { }.ToBytes();
            }
            catch (Exception ex)
            {
                return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes();
            }
        }

        public byte[] Register(IConnection connection)
        {
            if (!config.ConnectEnable)
            {
                return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.DISABLED }.ToBytes();
            }

            try
            {
                UdpForwardRegisterParamsInfo model = new UdpForwardRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Memory);

                //取出注册缓存，没取出来就说明没注册
                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    //限制的端口范围
                    if (model.SourcePort < config.TunnelListenRange.Min || model.SourcePort > config.TunnelListenRange.Max)
                    {
                        return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.OUT_RANGE, Msg = $"{config.TunnelListenRange.Min}-{config.TunnelListenRange.Max}" }.ToBytes();
                    }

                    UdpForwardTargetCacheInfo target = tcpForwardTargetCaching.Get(model.SourcePort);
                    //已存在相同的注册
                    if (target != null && target.Name != source.Name)
                    {
                        return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.EXISTS }.ToBytes();
                    }
                    tcpForwardTargetCaching.Add(model.SourcePort, new UdpForwardTargetCacheInfo
                    {
                        Name = source.Name,
                        Connection = connection,
                        Endpoint = NetworkHelper.EndpointToArray(model.TargetIp, model.TargetPort),
                        TunnelType = UdpForwardTunnelTypes.TCP_FIRST
                    });
                    try
                    {
                        tcpForwardServer.Start(model.SourcePort);
                    }
                    catch (Exception)
                    {
                        tcpForwardTargetCaching.Remove(model.SourcePort);
                    }
                }
                return new UdpForwardRegisterResult { }.ToBytes();
            }
            catch (Exception ex)
            {
                return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes();
            }
        }
    }
}
