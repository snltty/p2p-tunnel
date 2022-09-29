using common.server;
using common.udpforward;
using server.messengers.register;
using System.Linq;

namespace server.service.udpforward
{
    public class ServerUdpForwardValidator : DefaultUdpForwardValidator, IUdpForwardValidator
    {
        private readonly common.udpforward.Config config;
        private readonly KeysConfig keysConfig;
        private readonly IClientRegisterCaching clientRegisterCaching;

        public ServerUdpForwardValidator(common.udpforward.Config config, KeysConfig keysConfig, IClientRegisterCaching clientRegisterCaching) : base(config)
        {
            this.config = config;
            this.keysConfig = keysConfig;
            this.clientRegisterCaching = clientRegisterCaching;
        }
        public new bool Validate(IConnection connection)
        {
            if (config.ConnectEnable)
            {
                return config.ConnectEnable;
            }
            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo info))
            {
                return keysConfig.UdpForward.Contains(info.Key);
            }

            return false;
        }
    }

}
