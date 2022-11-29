using System;

namespace common.server.model
{

    [Flags]
    public enum EnumServiceAccess : uint
    {
        Register = 0,
        Relay = 1,
        TcpForward = 2,
        UdpForward = 4,
        Socks5 = 8,
        Setting = 16
    }

    [Flags, MessengerIdEnum]
    public enum ServiceAccessValidatorMessengerIds : ushort
    {
        Min = 1200,
        GetSetting = 1201,
        Setting = 1202,
        Max = 1299,
    }
}
