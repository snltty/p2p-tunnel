using System;

namespace server.messengers
{

    /// <summary>
    /// 服务端的各项服务权限
    /// </summary>
    [Flags]
    public enum EnumServiceAccess : uint
    {
        
        /// <summary>
        /// 登入
        /// </summary>
        SignIn = 0b00000000_00000000_00000000_00000100,
    }

}
