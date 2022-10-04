using System.Net;
using System;
using common.libs.extends;
using common.socks5;
using server.messengers;
using common.server;
using server.messengers.register;

namespace server.service.socks5
{
    public class Socks5Validator : ISocks5Validator
    {
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly common.socks5.Config config;
        private readonly IClientRegisterCaching clientRegisterCaching;

        public Socks5Validator(IServiceAccessValidator serviceAccessProvider, common.socks5.Config config, IClientRegisterCaching clientRegisterCaching)
        {
            this.serviceAccessProvider = serviceAccessProvider;
            this.config = config;
            this.clientRegisterCaching = clientRegisterCaching;
        }
        public bool Validate(IConnection connection, Socks5Info info)
        {
            return Validate(connection, info, config);
        }

        public bool Validate(IConnection connection, Socks5Info info, common.socks5.Config config)
        {
            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(info.Data, out Span<byte> ipMemory);
            if (remoteEndPoint.IsLan())
            {
                return false;
            }

            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                return config.ConnectEnable || serviceAccessProvider.Validate(client.GroupId, EnumService.Socks5);
            }

            return config.ConnectEnable;
        }
    }

}
