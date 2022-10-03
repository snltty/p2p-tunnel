using System.Net;
using System;
using common.libs.extends;
using common.socks5;
using server.messengers;

namespace server.service.socks5
{
    public class Socks5Validator : ISocks5Validator
    {
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly common.socks5.Config config;
        public Socks5Validator(IServiceAccessValidator serviceAccessProvider, common.socks5.Config config)
        {
            this.serviceAccessProvider = serviceAccessProvider;
            this.config = config;
        }
        public bool Validate(string key, Socks5Info info)
        {
            return Validate(key, info, config);
        }

        public bool Validate(string key, Socks5Info info, common.socks5.Config config)
        {
            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(info.Data, out Span<byte> ipMemory);
            if (remoteEndPoint.IsLan())
            {
                return false;
            }

            return config.ConnectEnable || serviceAccessProvider.Validate(key, EnumService.Socks5);
        }
    }

}
