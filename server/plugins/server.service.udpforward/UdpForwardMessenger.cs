using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.udpforward;
using server.messengers;
using server.messengers.register;
using System;
using System.Threading.Tasks;

namespace server.service.udpforward
{
    [MessengerIdRange((ushort)UdpForwardMessengerIds.Min, (ushort)UdpForwardMessengerIds.Max)]
    public class UdpForwardMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly common.udpforward.Config config;
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching;
        private readonly UdpForwardMessengerSender tcpForwardMessengerSender;
        private readonly IUdpForwardServer tcpForwardServer;
        private readonly IUdpForwardValidator udpForwardValidator;
        private readonly IServiceAccessValidator serviceAccessValidator;

        public UdpForwardMessenger(IClientRegisterCaching clientRegisterCache, common.udpforward.Config config, IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching,
            UdpForwardMessengerSender tcpForwardMessengerSender, IUdpForwardServer tcpForwardServer, IUdpForwardValidator udpForwardValidator, IServiceAccessValidator serviceAccessValidator)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.config = config;
            this.udpForwardTargetCaching = udpForwardTargetCaching;
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.tcpForwardServer = tcpForwardServer;
            this.udpForwardValidator = udpForwardValidator;
            this.serviceAccessValidator = serviceAccessValidator;
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

                    UdpForwardTargetCacheInfo cache = udpForwardTargetCaching.Get(port);
                    if (cache != null && cache.Id == source.Id)
                    {
                        udpForwardTargetCaching.Remove(port);
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
        public byte[] SignIn(IConnection connection)
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

                    UdpForwardTargetCacheInfo target = udpForwardTargetCaching.Get(model.SourcePort);
                    //已存在相同的注册
                    if (target != null && target.Name != source.Name)
                    {
                        return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.EXISTS }.ToBytes();
                    }
                    udpForwardTargetCaching.Add(model.SourcePort, new UdpForwardTargetCacheInfo
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
                        udpForwardTargetCaching.Remove(model.SourcePort);
                    }
                }
                return new UdpForwardRegisterResult { }.ToBytes();
            }
            catch (Exception ex)
            {
                return new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes();
            }
        }


        [MessengerId((ushort)UdpForwardMessengerIds.GetSetting)]
        public async Task<byte[]> GetSetting(IConnection connection)
        {
            return (await config.ReadString()).ToBytes();
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Setting)]
        public async Task<byte[]> Setting(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo client) == false)
            {
                return Helper.FalseArray;
            }
            if (serviceAccessValidator.Validate(client.GroupId, EnumServiceAccess.Setting) == false)
            {
                return Helper.FalseArray;
            }
            string jsonStr = connection.ReceiveRequestWrap.Payload.GetString();

            await config.SaveConfig(jsonStr);

            return Helper.TrueArray;
        }
    }
}
