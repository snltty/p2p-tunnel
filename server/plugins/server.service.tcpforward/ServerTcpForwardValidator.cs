using common.server.model;
using common.tcpforward;
using server.messengers;

namespace server.service.tcpforward
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ServerTcpForwardValidator : DefaultTcpForwardValidator, ITcpForwardValidator
    {
        private readonly common.tcpforward.Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serviceAccessProvider"></param>
        public ServerTcpForwardValidator(common.tcpforward.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new bool Validate(string key)
        {
            return config.ConnectEnable || serviceAccessProvider.Validate(key, EnumServiceAccess.TcpForward);
        }
    }

}
