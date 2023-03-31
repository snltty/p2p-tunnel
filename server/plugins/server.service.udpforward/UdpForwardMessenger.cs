using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.udpforward;
using server.messengers;
using server.messengers.singnin;
using System;
using System.Threading.Tasks;

namespace server.service.udpforward
{
    [MessengerIdRange((ushort)UdpForwardMessengerIds.Min, (ushort)UdpForwardMessengerIds.Max)]
    public sealed class UdpForwardMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCache;
        private readonly common.udpforward.Config config;
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching;
        private readonly UdpForwardMessengerSender tcpForwardMessengerSender;
        private readonly IUdpForwardServer tcpForwardServer;
        private readonly IUdpForwardValidator udpForwardValidator;
        private readonly IServiceAccessValidator serviceAccessValidator;

        public UdpForwardMessenger(IClientSignInCaching clientSignInCache, common.udpforward.Config config, IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching,
            UdpForwardMessengerSender tcpForwardMessengerSender, IUdpForwardServer tcpForwardServer, IUdpForwardValidator udpForwardValidator, IServiceAccessValidator serviceAccessValidator)
        {
            this.clientSignInCache = clientSignInCache;
            this.config = config;
            this.udpForwardTargetCaching = udpForwardTargetCaching;
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.tcpForwardServer = tcpForwardServer;
            this.udpForwardValidator = udpForwardValidator;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            await tcpForwardMessengerSender.OnRequest(data);
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            UdpForwardInfo data = new UdpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            await tcpForwardMessengerSender.OnResponse(data);
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Ports)]
        public void Ports(IConnection connection)
        {
            connection.Write(new ushort[] {
                config.TunnelListenRange.Min,
                    config.TunnelListenRange.Max
                });
        }

        [MessengerId((ushort)UdpForwardMessengerIds.SignOut)]
        public void SignOut(IConnection connection)
        {
            try
            {
                ushort port = connection.ReceiveRequestWrap.Payload.Span.ToUInt16();
                if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
                {
                    if (udpForwardValidator.Validate(connection) == false)
                    {
                        connection.Write(new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.DISABLED }.ToBytes());
                        return;
                    }

                    UdpForwardTargetCacheInfo cache = udpForwardTargetCaching.Get(port);
                    if (cache != null && cache.Id == source.ConnectionId)
                    {
                        udpForwardTargetCaching.Remove(port);
                        tcpForwardServer.Stop(port);
                    }
                }
                connection.Write(new UdpForwardRegisterResult { }.ToBytes());
                return;
            }
            catch (Exception ex)
            {
                connection.Write(new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes());
                return;
            }
        }

        [MessengerId((ushort)UdpForwardMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            try
            {
                UdpForwardRegisterParamsInfo model = new UdpForwardRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                //取出注册缓存，没取出来就说明没注册
                if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
                {
                    if (udpForwardValidator.Validate(connection) == false)
                    {
                        connection.Write(new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.DISABLED }.ToBytes());
                        return;
                    }

                    //限制的端口范围
                    if (model.SourcePort < config.TunnelListenRange.Min || model.SourcePort > config.TunnelListenRange.Max)
                    {
                        connection.Write(new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.OUT_RANGE, Msg = $"{config.TunnelListenRange.Min}-{config.TunnelListenRange.Max}" }.ToBytes());
                        return;
                    }

                    UdpForwardTargetCacheInfo target = udpForwardTargetCaching.Get(model.SourcePort);
                    //已存在相同的注册
                    if (target != null && target.Name != source.Name)
                    {
                        connection.Write(new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.EXISTS }.ToBytes());
                        return;
                    }
                    udpForwardTargetCaching.Add(model.SourcePort, new UdpForwardTargetCacheInfo
                    {
                        Id = source.ConnectionId,
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
                connection.Write(new UdpForwardRegisterResult { }.ToBytes());
                return;
            }
            catch (Exception ex)
            {
                connection.Write(new UdpForwardRegisterResult { Code = UdpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes());
                return;
            }
        }

        [MessengerId((ushort)UdpForwardMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            connection.WriteUTF8(await config.ReadString());
        }

        [MessengerId((ushort)UdpForwardMessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (serviceAccessValidator.Validate(connection, (uint)EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            string jsonStr = connection.ReceiveRequestWrap.Payload.GetUTF8String();

            await config.SaveConfig(jsonStr);

            connection.Write(Helper.TrueArray);
        }
    }
}
