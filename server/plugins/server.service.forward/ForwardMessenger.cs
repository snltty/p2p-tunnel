using common.forward;
using common.libs;
using common.libs.extends;
using common.proxy;
using common.server;
using server.messengers;
using server.messengers.singnin;
using server.service.forward;
using server.service.forward.model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.forward
{
    [MessengerIdRange((ushort)ForwardMessengerIds.Min, (ushort)ForwardMessengerIds.Max)]
    public sealed class ForwardMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCache;
        private readonly common.forward.Config config;
        private readonly IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly IProxyServer proxyServer;
        private readonly IForwardProxyPlugin forwardProxyPlugin;

        public ForwardMessenger(IClientSignInCaching clientSignInCache, common.forward.Config config,
            IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching, IServiceAccessValidator serviceAccessValidator, IProxyServer proxyServer, IForwardProxyPlugin forwardProxyPlugin)
        {
            this.clientSignInCache = clientSignInCache;
            this.config = config;
            this.forwardTargetCaching = forwardTargetCaching;
            this.serviceAccessValidator = serviceAccessValidator;
            this.proxyServer = proxyServer;
            this.forwardProxyPlugin = forwardProxyPlugin;
        }


        [MessengerId((ushort)ForwardMessengerIds.Domains)]
        public void Domains(IConnection connection)
        {
            connection.WriteUTF8(config.Domains.ToJson());
        }

        [MessengerId((ushort)ForwardMessengerIds.Ports)]
        public void Ports(IConnection connection)
        {
            connection.Write(config.WebListens
                .Concat(new ushort[] {
                    config.TunnelListenRange.Min,
                    config.TunnelListenRange.Max
                }).ToArray());
        }

        [MessengerId((ushort)ForwardMessengerIds.SignOut)]
        public void SignOut(IConnection connection)
        {
            try
            {
                ForwardSignOutInfo model = new ForwardSignOutInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
                {
                    ForwardTargetCacheInfo cache = model.AliveType == ForwardAliveTypes.Web ? forwardTargetCaching.Get(model.SourceIp, model.SourcePort) : forwardTargetCaching.Get(model.SourcePort);

                    if (cache != null && cache.ConnectionId == source.ConnectionId)
                    {
                        if (model.AliveType == ForwardAliveTypes.Web)
                        {
                            forwardTargetCaching.Remove(model.SourceIp, model.SourcePort);
                        }
                        else
                        {
                            forwardTargetCaching.Remove(model.SourcePort);
                            proxyServer.Stop(model.SourcePort);
                        }
                    }
                }
                connection.Write(new ForwardSignInResultInfo { }.ToBytes());
                return;
            }
            catch (Exception ex)
            {
                connection.Write(new ForwardSignInResultInfo { Code = ForwardSignInResultCodes.UNKNOW, Msg = ex.Message }.ToBytes());
                return;
            }
        }


        [MessengerId((ushort)ForwardMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            try
            {
                ForwardSignInInfo model = new ForwardSignInInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Payload);

                //取出注册缓存，没取出来就说明没注册
                if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
                {
                    if (config.ConnectEnable == false && serviceAccessValidator.Validate(connection.ConnectId, forwardProxyPlugin.Access) == false)
                    {
                        connection.Write(new ForwardSignInResultInfo { Code = ForwardSignInResultCodes.DISABLED }.ToBytes());
                        return;
                    }

                    //短连接转发注册，一个 host:port组合只能注册一次，被占用时不可再次注册
                    if (model.AliveType == ForwardAliveTypes.Web)
                    {
                        ForwardTargetCacheInfo target = forwardTargetCaching.Get(model.SourceIp, model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.ConnectionId != source.ConnectionId)
                        {
                            connection.Write(new ForwardSignInResultInfo { Code = ForwardSignInResultCodes.EXISTS }.ToBytes());
                            return;
                        }
                        forwardTargetCaching.AddOrUpdate(model.SourceIp, model.SourcePort, new ForwardTargetCacheInfo
                        {
                            ConnectionId = source.ConnectionId,
                            Connection = connection,
                            IPAddress = model.TargetIp.GetAddressBytes(),
                            Port = model.TargetPort,
                        });
                    }
                    //长连接转发注册，一个端口只能注册一次，被占用时不可再次注册
                    else
                    {
                        //限制的端口范围
                        if (model.SourcePort < config.TunnelListenRange.Min || model.SourcePort > config.TunnelListenRange.Max)
                        {
                            connection.Write(new ForwardSignInResultInfo { Code = ForwardSignInResultCodes.OUT_RANGE, Msg = $"{config.TunnelListenRange.Min}-{config.TunnelListenRange.Max}" }.ToBytes());
                            return;
                        }

                        ForwardTargetCacheInfo target = forwardTargetCaching.Get(model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.ConnectionId != source.ConnectionId)
                        {
                            connection.Write(new ForwardSignInResultInfo { Code = ForwardSignInResultCodes.EXISTS }.ToBytes());
                            return;
                        }
                        forwardTargetCaching.AddOrUpdate(model.SourcePort, new ForwardTargetCacheInfo
                        {
                            ConnectionId = source.ConnectionId,
                            Connection = connection,
                            IPAddress = model.TargetIp.GetAddressBytes(),
                            Port = model.TargetPort,
                        });
                        try
                        {
                            proxyServer.Start(model.SourcePort, config.Plugin,(byte)ForwardAliveTypes.Tunnel);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.DebugError(ex);
                            forwardTargetCaching.Remove(model.SourcePort);
                        }
                    }
                }
                connection.Write(new ForwardSignInResultInfo { }.ToBytes());
                return;
            }
            catch (Exception ex)
            {
                connection.Write(new ForwardSignInResultInfo { Code = ForwardSignInResultCodes.UNKNOW, Msg = ex.Message }.ToBytes());
                return;
            }
        }

        [MessengerId((ushort)ForwardMessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            string str = await config.ReadString();
            connection.WriteUTF8(str);
        }

        [MessengerId((ushort)ForwardMessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo client) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            if (serviceAccessValidator.Validate(connection.ConnectId, (uint)common.server.EnumServiceAccess.Setting) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            string jsonStr = connection.ReceiveRequestWrap.Payload.GetUTF8String();
            for (int i = 0; i < config.WebListens.Length; i++)
            {
                proxyServer.Stop(config.WebListens[i]);
            }

            await config.SaveConfig(jsonStr);

            for (int i = 0; i < config.WebListens.Length; i++)
            {
                proxyServer.Start(config.WebListens[i], config.Plugin, (byte)ForwardAliveTypes.Web);
            }

            connection.Write(Helper.TrueArray);
        }

    }
}
