using common.server;
using common.tcpforward;
using server.messengers.register;
using System.Linq;

namespace server.service.tcpforward
{
    public class ServerTcpForwardValidator : DefaultTcpForwardValidator, ITcpForwardValidator
    {
        private readonly common.tcpforward.Config config;
        private readonly KeysConfig keysConfig;
        private readonly IClientRegisterCaching clientRegisterCaching;
        public ServerTcpForwardValidator(common.tcpforward.Config config, KeysConfig keysConfig, IClientRegisterCaching clientRegisterCaching) : base(config)
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
                return keysConfig.TcpForward.Contains(info.Key);
            }

            return false;
        }
    }

}
