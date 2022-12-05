using common.server.model;
using common.udpforward;
using server.messengers;

namespace server.service.udpforward
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ServerUdpForwardValidator : DefaultUdpForwardValidator, IUdpForwardValidator
    {
        private readonly common.udpforward.Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serviceAccessProvider"></param>
        public ServerUdpForwardValidator(common.udpforward.Config config, IServiceAccessValidator serviceAccessProvider) : base(config)
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
            return config.ConnectEnable || serviceAccessProvider.Validate(key, EnumServiceAccess.UdpForward);
        }
    }

}
