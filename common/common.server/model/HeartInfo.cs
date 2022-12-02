using System;

namespace common.server.model
{
    [Flags, MessengerIdEnum]
    public enum HeartMessengerIds : ushort
    {
        Min = 300,
        Alive = 301,
        Max = 399,
    }
}
