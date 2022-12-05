using server.messengers.register;
using server.messengers;
using common.server.model;

namespace server.service.validators
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RegisterValidator : IRegisterKeyValidator
    {
        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessProvider;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="serviceAccessProvider"></param>
        public RegisterValidator(Config config, IServiceAccessValidator serviceAccessProvider)
        {
            this.config = config;
            this.serviceAccessProvider = serviceAccessProvider;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Validate(string key)
        {
            return config.RegisterEnable || serviceAccessProvider.Validate(key, EnumServiceAccess.Register);
        }
    }


}
