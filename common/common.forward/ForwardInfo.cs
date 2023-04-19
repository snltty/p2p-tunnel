using common.server;
using System;
using System.ComponentModel;

namespace common.forward
{

    [Flags]
    public enum ForwardAliveTypes : byte
    {
        [Description("长连接")]
        Tunnel = 0,
        [Description("短连接")]
        Web = 1
    }

    /// <summary>
    /// tcp转发相关的消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum ForwardMessengerIds : ushort
    {
        Min = 600,
        /// <summary>
        /// 获取端口
        /// </summary>
        Ports = 604,
        /// <summary>
        /// 注册
        /// </summary>
        SignIn = 605,
        /// <summary>
        /// 退出
        /// </summary>
        SignOut = 606,
        /// <summary>
        /// 获取配置
        /// </summary>
        GetSetting = 607,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 608,
        Max = 699,
    }
}
