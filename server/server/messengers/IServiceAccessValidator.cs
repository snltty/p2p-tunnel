using common.server;
using server.messengers.singnin;

namespace server.messengers
{
    public interface IServiceAccessValidator
    {
        /// <summary>
        /// 验证权限
        /// </summary>
        /// <param name="connection">连接对象</param>
        /// <param name="service">验证哪个权限</param>
        /// <returns></returns>
        public bool Validate(IConnection connection, uint service);
        /// <summary>
        /// 验证权限
        /// </summary>
        /// <param name="connectionid">连接id</param>
        /// <param name="service">验证哪个权限</param>
        /// <returns></returns>
        public bool Validate(ulong connectionid, uint service);
        /// <summary>
        /// 验证权限
        /// </summary>
        /// <param name="cache">缓存对象</param>
        /// <param name="service">验证哪个权限</param>
        /// <returns></returns>
        public bool Validate(SignInCacheInfo cache, uint service);
        /// <summary>
        /// 验证权限
        /// </summary>
        /// <param name="access">你有的权限</param>
        /// <param name="service">验证哪个权限</param>
        /// <returns></returns>
        public bool Validate(uint access, uint service);
    }

}
