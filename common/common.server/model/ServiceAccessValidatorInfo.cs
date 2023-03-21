using System;

namespace common.server.model
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
        SignIn = 0,
        /// <summary>
        /// 中继
        /// </summary>
        Relay = 1,
        /// <summary>
        /// tcp转发
        /// </summary>
        TcpForward = 2,
        /// <summary>
        /// udp转发
        /// </summary>
        UdpForward = 4,
        /// <summary>
        /// socks5
        /// </summary>
        Socks5 = 8,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 16
    }

    /// <summary>
    /// 权限相关的消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum ServiceAccessValidatorMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 1200,
        /// <summary>
        /// 获取配置
        /// </summary>
        GetSetting = 1201,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 1202,
        /// <summary>
        /// 
        /// </summary>
        Max = 1299,
    }
}
