using common.libs;
using common.libs.extends;
using common.server;
using common.tcpforward;
using server.messengers.register;
using System;
using System.Linq;
using System.Net;

namespace server.service.tcpforward
{
    public class TcpForwardMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly common.tcpforward.Config config;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        private readonly ITcpForwardServer tcpForwardServer;

        public TcpForwardMessenger(IClientRegisterCaching clientRegisterCache, common.tcpforward.Config config, ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching, TcpForwardMessengerSender tcpForwardMessengerSender, ITcpForwardServer tcpForwardServer)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.config = config;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.tcpForwardServer = tcpForwardServer;
        }

        public void Request(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            tcpForwardMessengerSender.OnRequest(data);
        }

        public void Response(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            tcpForwardMessengerSender.OnResponse(data);
        }

        public int[] GetPorts(IConnection connection)
        {
            return config.WebListens
                .Concat(new int[] {
                    config.TunnelListenRange.Min,
                    config.TunnelListenRange.Max
                }).ToArray();
        }

        public TcpForwardRegisterResult UnRegister(IConnection connection)
        {
            if (!config.ConnectEnable)
            {
                return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.DISABLED };
            }

            try
            {
                TcpForwardUnRegisterParamsInfo model = new TcpForwardUnRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Memory);

                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    if (model.AliveType == TcpForwardAliveTypes.WEB)
                    {
                        tcpForwardTargetCaching.Remove(model.SourceIp, model.SourcePort);
                    }
                    else
                    {
                        tcpForwardTargetCaching.Remove(model.SourcePort);
                        tcpForwardServer.Stop(model.SourcePort);
                    }
                }
                return new TcpForwardRegisterResult { };
            }
            catch (Exception ex)
            {
                return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message };
            }
        }

        public TcpForwardRegisterResult Register(IConnection connection)
        {
            if (!config.ConnectEnable)
            {
                return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.DISABLED };
            }

            try
            {
                TcpForwardRegisterParamsInfo model = new TcpForwardRegisterParamsInfo();
                model.DeBytes(connection.ReceiveRequestWrap.Memory);

                //取出注册缓存，没取出来就说明没注册
                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    //短连接转发注册，一个 host:port组合只能注册一次，被占用时不可再次注册
                    if (model.AliveType == TcpForwardAliveTypes.WEB)
                    {
                        TcpForwardTargetCacheInfo target = tcpForwardTargetCaching.Get(model.SourceIp, model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.Name != source.Name)
                        {
                            return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.EXISTS };
                        }
                        tcpForwardTargetCaching.AddOrUpdate(model.SourceIp, model.SourcePort, new TcpForwardTargetCacheInfo
                        {
                            Name = source.Name,
                            Connection = connection,
                            Endpoint = NetworkHelper.EndpointToArray(model.TargetIp, model.TargetPort),
                            ForwardType = TcpForwardTypes.FORWARD,
                            TunnelType = TcpForwardTunnelTypes.TCP_FIRST
                        });
                    }
                    //长连接转发注册，一个端口只能注册一次，被占用时不可再次注册
                    else
                    {
                        //限制的端口范围
                        if (model.SourcePort < config.TunnelListenRange.Min || model.SourcePort > config.TunnelListenRange.Max)
                        {
                            return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.OUT_RANGE, Msg = $"{config.TunnelListenRange.Min}-{config.TunnelListenRange.Max}" };
                        }

                        TcpForwardTargetCacheInfo target = tcpForwardTargetCaching.Get(model.SourcePort);
                        //已存在相同的注册
                        if (target != null && target.Name != source.Name)
                        {
                            return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.EXISTS };
                        }
                        tcpForwardTargetCaching.AddOrUpdate(model.SourcePort, new TcpForwardTargetCacheInfo
                        {
                            Name = source.Name,
                            Connection = connection,
                            Endpoint = NetworkHelper.EndpointToArray(model.TargetIp, model.TargetPort),
                            ForwardType = TcpForwardTypes.FORWARD,
                            TunnelType = TcpForwardTunnelTypes.TCP_FIRST
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
                return new TcpForwardRegisterResult { };
            }
            catch (Exception ex)
            {
                return new TcpForwardRegisterResult { Code = TcpForwardRegisterResultCodes.UNKNOW, Msg = ex.Message };
            }
        }
    }
}
