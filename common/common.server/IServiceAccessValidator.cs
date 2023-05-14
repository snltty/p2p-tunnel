using System;

namespace common.server
{
    public interface IAccess
    {
        /// <summary>
        /// 权限
        /// </summary>
        public uint Access { get; }
        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; }
    }

    public interface IServiceAccessValidator
    {
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

    public class ServiceAccessValidator : IServiceAccessValidator
    {

        public ServiceAccessValidator()
        {
        }

        public virtual bool Validate(ulong connectionid, uint service)
        {
            return false;
        }
        public bool Validate(uint access, uint service)
        {
            return (access & service) == service;
        }
    }


    [Flags]
    public enum EnumServiceAccess : uint
    {
        None = 0b00000000_00000000_00000000_00000000,
        /// <summary>
        /// 中继
        /// </summary>
        Relay = 0b00000000_00000000_00000000_00000001,
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
