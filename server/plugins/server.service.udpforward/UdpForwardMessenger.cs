using common.libs;
using common.libs.extends;
using common.server;
using common.udpforward;
using server.messengers.register;
using System;

namespace server.service.udpforward
{
    [MessengerIdRange((ushort)UdpForwardMessengerIds.Min, (ushort)UdpForwardMessengerIds.Max)]
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

        [MessengerId((ushort)UdpForwardMessengerIds.Request)]
        public void Request(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            tcpForwardMessengerSender.OnRequest(data);
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            tcpForwardMessengerSender.OnResponse(data);
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Ports)]
        public byte[] Ports(IConnection connection)
        {
            return new ushort[] {
                config.TunnelListenRange.Min,
                    config.TunnelListenRange.Max
                }.ToBytes();
        }

        [MessengerId((ushort)UdpForwardMessengerIds.SignOut)]
        public byte[] SignOut(IConnection connection)
        {
            try
            {
                ushort port = connection.ReceiveRequestWrap.Payload.Span.ToUInt16();
                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    if (udpForwardValidator.Validate(source.GroupId) == false)
                    {
                        return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.DISABLED }.ToBytes();
                    }

                    UdpForwardTargetCacheInfo cache = tcpForwardTargetCaching.Get(port);
                    if (cache != null && cache.Id == source.Id)
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

        [MessengerId((ushort)UdpForwardMessengerIds.SignIn)]
        public byte[] Register(IConnection connection)
        {
            try
            {
                UdpForwardRegisterParamsInfo model = new UdpForwardRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                //取出注册缓存，没取出来就说明没注册
                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    if (udpForwardValidator.Validate(source.GroupId) == false)
                    {
                        return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.DISABLED }.ToBytes();
                    }

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
                        Id = source.Id,
                        Name = source.Name,
                        Connection = connection,
                        Endpoint = NetworkHelper.EndpointToArray(model.TargetIp, model.TargetPort)
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
