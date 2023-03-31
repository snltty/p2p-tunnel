using System;

namespace common.server.model
{

    /// <summary>
    /// 服务端的各项服务权限
    /// </summary>
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
        Setting = 0b00000000_00000000_00000000_00000010
    }

}
