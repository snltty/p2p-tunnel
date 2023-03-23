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
        SignIn = 1,
        /// <summary>
        /// 中继
        /// </summary>
        Relay = 2,
        /// <summary>
        /// tcp转发
        /// </summary>
        TcpForward = 4,
        /// <summary>
        /// udp转发
        /// </summary>
        UdpForward = 8,
        /// <summary>
        /// socks5
        /// </summary>
        Socks5 = 16,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 32
    }

}
