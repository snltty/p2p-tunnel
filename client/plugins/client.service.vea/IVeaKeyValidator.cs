using common.socks5;

namespace client.service.vea
{

    /// <summary>
    /// 组网权限验证
    /// </summary>
    public interface IVeaKeyValidator : ISocks5Validator
    {
    }

    /// <summary>
    /// 组网权限验证
    /// </summary>
    public sealed class DefaultVeaKeyValidator: IVeaKeyValidator
    {
        private Config config;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public DefaultVeaKeyValidator(Config config)
        {
            this.config = config;
        }

        public bool Validate(Socks5Info info)
        {
            return config.ConnectEnable;
        }
    }

}
