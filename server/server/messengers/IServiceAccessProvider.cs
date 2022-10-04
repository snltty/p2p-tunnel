using System;

namespace server.messengers
{
    public interface IServiceAccessValidator
    {
        public bool Validate(string group, EnumService service);
    }

    [Flags]
    public enum EnumService : uint
    {
        Register = 0,
        Relay = 1,
        TcpForward = 2,
        UdpForward = 4,
        Socks5 = 8
    }
}
