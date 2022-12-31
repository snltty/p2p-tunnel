using System.Net;
using System;
using common.libs.extends;
using common.socks5;
using server.messengers;
using common.server;
using server.messengers.register;
using common.server.model;

namespace server.service.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Socks5Validator : ISocks5Validator
    {
        private readonly IServiceAccessValidator serviceAccessProvider;
        private readonly common.socks5.Config config;
        private readonly IClientRegisterCaching clientRegisterCaching;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceAccessProvider"></param>
        /// <param name="config"></param>
        /// <param name="clientRegisterCaching"></param>
        public Socks5Validator(IServiceAccessValidator serviceAccessProvider, common.socks5.Config config, IClientRegisterCaching clientRegisterCaching)
        {
            this.serviceAccessProvider = serviceAccessProvider;
            this.config = config;
            this.clientRegisterCaching = clientRegisterCaching;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Validate( Socks5Info info)
        {
            return Validate(info, config);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="info"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Validate(Socks5Info info, common.socks5.Config config)
        {
            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(info.Data, out Span<byte> ipMemory);
            if (remoteEndPoint.IsLan())
            {
                return false;
            }

            if (clientRegisterCaching.Get(info.ClientId, out RegisterCacheInfo client))
            {
                return config.ConnectEnable || serviceAccessProvider.Validate(client.GroupId, EnumServiceAccess.Socks5);
            }

            return config.ConnectEnable;
        }
    }

}
