using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.tcpforward;
using server.messengers;
using server.messengers.register;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.tcpforward
{
    /// <summary>
    /// 
    /// </summary>
    [MessengerIdRange((ushort)TcpForwardMessengerIds.Min, (ushort)TcpForwardMessengerIds.Max)]
    public sealed class TcpForwardMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly common.tcpforward.Config config;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly ITcpForwardServer tcpForwardServer;
        private readonly ITcpForwardValidator tcpForwardValidator;
        private readonly TcpForwardResolver tcpForwardResolver;
        private readonly IServiceAccessValidator serviceAccessValidator;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCache"></param>
        /// <param name="config"></param>
        /// <param name="tcpForwardTargetCaching"></param>
        /// <param name="tcpForwardServer"></param>
        /// <param name="tcpForwardValidator"></param>
        /// <param name="tcpForwardResolver"></param>
        /// <param name="serviceAccessValidator"></param>
        public TcpForwardMessenger(IClientRegisterCaching clientRegisterCache, common.tcpforward.Config config,
            ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching, ITcpForwardServer tcpForwardServer,
            ITcpForwardValidator tcpForwardValidator, TcpForwardResolver tcpForwardResolver, IServiceAccessValidator serviceAccessValidator)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.config = config;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.tcpForwardServer = tcpForwardServer;
            this.tcpForwardValidator = tcpForwardValidator;
            this.tcpForwardResolver = tcpForwardResolver;
            this.serviceAccessValidator = serviceAccessValidator;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TcpForwardMessengerIds.Request)]
        public void Request(IConnection connection)
        {
            tcpForwardResolver.InputData(connection);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TcpForwardMessengerIds.Response)]
        public void Response(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Payload);
            tcpForwardServer.Response(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TcpForwardMessengerIds.Ports)]
        public void Ports(IConnection connection)
        {
            connection.Write(config.WebListens
                .Concat(new ushort[] {
                    config.TunnelListenRange.Min,
                    config.TunnelListenRange.Max
                }).ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TcpForwardMessengerIds.SignOut)]
        public byte[] SignOut(IConnection connection)
        {
            try
            {
                TcpForwardUnRegisterParamsInfo model = new TcpForwardUnRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    TcpForwardTargetCacheInfo cache = model.AliveType == TcpForwardAliveTypes.Web ? tcpForwardTargetCaching.Get(model.SourceIp, model.SourcePort) : tcpForwardTargetCaching.Get(model.SourcePort);

                    if (cache != null && cache.Id == source.Id)
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
                return new TcpForwardRegisterResult { }.ToBytes();
            }
            catch (Exception ex)
            {
                return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TcpForwardMessengerIds.SignIn)]
        public byte[] SignIn(IConnection connection)
        {
            try
            {
                TcpForwardRegisterParamsInfo model = new TcpForwardRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                //取出注册缓存，没取出来就说明没注册
                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    if (tcpForwardValidator.Validate(source.GroupId) == false)
                    {
                        return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.DISABLED }.ToBytes();
                    }

                    //短连接转发注册，一个 host:port组合只能注册一次，被占用时不可再次注册
                    if (model.AliveType == TcpForwardAliveTypes.Web)
                    {
                        TcpForwardTargetCacheInfo target = tcpForwardTargetCaching.Get(model.SourceIp, model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.Id != source.Id)
                        {
                            return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.EXISTS }.ToBytes();
                        }
                        tcpForwardTargetCaching.AddOrUpdate(model.SourceIp, model.SourcePort, new TcpForwardTargetCacheInfo
                        {
                            Id = source.Id,
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
                            return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.OUT_RANGE, Msg = $"{config.TunnelListenRange.Min}-{config.TunnelListenRange.Max}" }.ToBytes();
                        }

                        TcpForwardTargetCacheInfo target = tcpForwardTargetCaching.Get(model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.Id != source.Id)
                        {
                            return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.EXISTS }.ToBytes();
                        }
                        tcpForwardTargetCaching.AddOrUpdate(model.SourcePort, new TcpForwardTargetCacheInfo
                        {
                            Id = source.Id,
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
                return new TcpForwardRegisterResult { }.ToBytes();
            }
            catch (Exception ex)
            {
                return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message }.ToBytes();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TcpForwardMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            string str = await config.ReadString();
            connection.WriteUTF8(str);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TcpForwardMessengerIds.Setting)]
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

            return Helper.TrueArray;
        }

    }
}
