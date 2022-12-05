using common.server;

namespace common.server
{
    /// <summary>
    /// 中继权限验证
    /// </summary>
    public interface IRelayValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool Validate(IConnection connection);
    }

    /// <summary>
    /// 默认的验证
    /// </summary>
    public sealed class DefaultRelayValidator : IRelayValidator
    {
        /// <summary>
        /// 
        /// </summary>
        public DefaultRelayValidator()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool Validate(IConnection connection)
        {
            return true;
        }
    }

}
