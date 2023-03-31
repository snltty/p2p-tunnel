using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.tcpforward;
using server.messengers;
using server.messengers.singnin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.tcpforward
{
    [MessengerIdRange((ushort)TcpForwardMessengerIds.Min, (ushort)TcpForwardMessengerIds.Max)]
    public sealed class TcpForwardMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCache;
        private readonly common.tcpforward.Config config;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly ITcpForwardServer tcpForwardServer;
        private readonly ITcpForwardValidator tcpForwardValidator;
        private readonly TcpForwardResolver tcpForwardResolver;
        private readonly IServiceAccessValidator serviceAccessValidator;
        public TcpForwardMessenger(IClientSignInCaching clientSignInCache, common.tcpforward.Config config,
            ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching, ITcpForwardServer tcpForwardServer,
            ITcpForwardValidator tcpForwardValidator, TcpForwardResolver tcpForwardResolver, IServiceAccessValidator serviceAccessValidator)
        {
            this.clientSignInCache = clientSignInCache;
            this.config = config;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.tcpForwardServer = tcpForwardServer;
            this.tcpForwardValidator = tcpForwardValidator;
            this.tcpForwardResolver = tcpForwardResolver;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        [MessengerId((ushort)TcpForwardMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            await tcpForwardResolver.InputData(connection);
        }

        [MessengerId((ushort)TcpForwardMessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            await tcpForwardServer.Response(data);
        }


        [MessengerId((ushort)TcpForwardMessengerIds.Ports)]
        public void Ports(IConnection connection)
        {
            connection.Write(config.WebListens
                .Concat(new ushort[] {
                    config.TunnelListenRange.Min,
                    config.TunnelListenRange.Max
                }).ToArray());
        }

        [MessengerId((ushort)TcpForwardMessengerIds.SignOut)]
        public void SignOut(IConnection connection)
        {
            try
            {
                TcpForwardUnRegisterParamsInfo model = new TcpForwardUnRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
                {
                    TcpForwardTargetCacheInfo cache = model.AliveType == TcpForwardAliveTypes.Web ? tcpForwardTargetCaching.Get(model.SourceIp, model.SourcePort) : tcpForwardTargetCaching.Get(model.SourcePort);

                    if (cache != null && cache.Id == source.ConnectionId)
                    {
                        if (model.AliveType == TcpForwardAliveTypes.Web)
                        {
                            tcpForwardTargetCaching.Remove(model.SourceIp, model.SourcePort);
                        }
                        else
                        {
                            tcpForwardTargetCaching.Remove(model.SourcePort);
                            tcpForwardServer.Stop(model.SourcePort);
                        }
                    }
                }
                connection.Write(new TcpForwardRegisterResult { }.ToBytes());
                return ;
            }
            catch (Exception ex)
            {
                connection.Write(new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes());
                return ;
            }
        }


        [MessengerId((ushort)TcpForwardMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            try
            {
                TcpForwardRegisterParamsInfo model = new TcpForwardRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                //取出注册缓存，没取出来就说明没注册
                if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
                {
                    if (tcpForwardValidator.Validate(connection) == false)
                    {
                        connection.Write(new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.DISABLED }.ToBytes());
                        return ;
                    }

                    //短连接转发注册，一个 host:port组合只能注册一次，被占用时不可再次注册
                    if (model.AliveType == TcpForwardAliveTypes.Web)
                    {
                        TcpForwardTargetCacheInfo target = tcpForwardTargetCaching.Get(model.SourceIp, model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.Id != source.ConnectionId)
                        {
                            connection.Write(new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.EXISTS }.ToBytes());
                            return ;
                        }
                        tcpForwardTargetCaching.AddOrUpdate(model.SourceIp, model.SourcePort, new TcpForwardTargetCacheInfo
                        {
                            Id = source.ConnectionId,
                            Name = source.Name,
                            Connection = connection,
                            Endpoint = NetworkHelper.EndpointToArray(model.TargetIp, model.TargetPort),
                            ForwardType = TcpForwardTypes.Forward
                        });
                    }
                    //长连接转发注册，一个端口只能注册一次，被占用时不可再次注册
                    else
                    {
                        //限制的端口范围
                        if (model.SourcePort < config.TunnelListenRange.Min || model.SourcePort > config.TunnelListenRange.Max)
                        {
                            connection.Write(new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.OUT_RANGE, Msg = $"{config.TunnelListenRange.Min}-{config.TunnelListenRange.Max}" }.ToBytes());
                            return ;
                        }

                        TcpForwardTargetCacheInfo target = tcpForwardTargetCaching.Get(model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.Id != source.ConnectionId)
                        {
                            connection.Write(new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.EXISTS }.ToBytes());
                            return ;
                        }
                        tcpForwardTargetCaching.AddOrUpdate(model.SourcePort, new TcpForwardTargetCacheInfo
                        {
                            Id = source.ConnectionId,
                            Name = source.Name,
                            Connection = connection,
                            Endpoint = NetworkHelper.EndpointToArray(model.TargetIp, model.TargetPort),
                            ForwardType = TcpForwardTypes.Forward
                        });
                        try
                        {
                            tcpForwardServer.Start(model.SourcePort, model.AliveType);
                        }
                        catch (Exception)
                        {
                            tcpForwardTargetCaching.Remove(model.SourcePort);
                        }
                    }
                }
                connection.Write(new TcpForwardRegisterResult { }.ToBytes());
                return ;
            }
            catch (Exception ex)
            {
                connection.Write(new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes());
                return ;
            }
        }

        [MessengerId((ushort)TcpForwardMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            string str = await config.ReadString();
            connection.WriteUTF8(str);
        }

        [MessengerId((ushort)TcpForwardMessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return ;
            }
            if (serviceAccessValidator.Validate(connection, (uint)EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return ;
            }

            string jsonStr = connection.ReceiveRequestWrap.Payload.GetUTF8String();
            for (int i = 0; i < config.WebListens.Length; i++)
            {
                tcpForwardServer.Stop(config.WebListens[i]);
            }

            await config.SaveConfig(jsonStr);

            for (int i = 0; i < config.WebListens.Length; i++)
            {
                tcpForwardServer.Start(config.WebListens[i], TcpForwardAliveTypes.Web);
            }

            connection.Write(Helper.TrueArray);
        }

    }
}
