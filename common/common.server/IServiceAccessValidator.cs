using System;
using System.Collections.Generic;

namespace common.server
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
        /// <param name="access">你有的权限</param>
        /// <param name="service">验证哪个权限</param>
        /// <returns></returns>
        public bool Validate(uint access, uint service);

    }


    [Flags]
    public enum EnumServiceAccess : uint
    {
        None = 0b00000000_00000000_00000000_00000000,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 0b00000000_00000000_00000000_00000010,
        /// <summary>
        /// 全部权限
        /// </summary>
        All = uint.MaxValue
    }
}
