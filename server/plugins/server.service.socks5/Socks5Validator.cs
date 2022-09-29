using System.Net;
using System;
using common.libs.extends;
using common.socks5;
using server.messengers.register;
using System.Linq;
using common.server;

namespace server.service.socks5
{
    public class Socks5Validator : ISocks5Validator
    {
        private readonly KeysConfig keysConfig;
        private readonly IClientRegisterCaching clientRegisterCaching;
        public Socks5Validator(KeysConfig keysConfig, IClientRegisterCaching clientRegisterCaching)
        {
            this.keysConfig = keysConfig;
            this.clientRegisterCaching = clientRegisterCaching;
        }
        public bool Validate(IConnection connection, Socks5Info info, common.socks5.Config config)
        {
            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo client) == false)
            {
                return false;
            }

            if (config.ConnectEnable == false)
            {
                return keysConfig.Socks5.Contains(client.Key);
            }

            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(info.Data, out Span<byte> ipMemory);
            if (config.LanConnectEnable == false && remoteEndPoint.IsLan())
            {
                return keysConfig.Socks5.Contains(client.Key);
            }

            return true;
        }
    }

}
