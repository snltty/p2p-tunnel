using common.server;

namespace client.realize.messengers.relay
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RelayValidator : IRelayValidator
    {
        private readonly Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public RelayValidator(Config config)
        {
            this.config = config;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool Validate(IConnection connection)
        {
            return config.Client.UseRelay;

        }
    }
}
