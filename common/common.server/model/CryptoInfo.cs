using System;

namespace common.server.model
{
    interface CryptoInfo
    {
    }

    [Flags, MessengerIdEnum]
    public enum CryptoMessengerIds:int
    {
        Min = 201,
        Key = 201,
        Set = 202,
        Test = 203,
        Clear = 204,

        Max = 300,
    }
}
