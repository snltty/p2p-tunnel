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
    public sealed class DefaultVeaKeyValidator : DefaultSocks5Validator, IVeaKeyValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public DefaultVeaKeyValidator(Config config) : base(new common.socks5.Config
        {
            BufferSize = config.BufferSize,
            ConnectEnable = config.ConnectEnable,
            NumConnections = config.NumConnections,
        })
        {

        }
    }

}
