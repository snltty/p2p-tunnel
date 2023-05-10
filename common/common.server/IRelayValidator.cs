using common.server;

namespace common.server
{
    /// <summary>
    /// 中继权限验证
    /// </summary>
    public interface IRelayValidator
    {
        public bool Validate(IConnection connection);
    }

    /// <summary>
    /// 默认的验证
    /// </summary>
    public sealed class DefaultRelayValidator : IRelayValidator
    {
        public DefaultRelayValidator()
        {
        }
        public bool Validate(IConnection connection)
        {
            return true;
        }
    }

}
