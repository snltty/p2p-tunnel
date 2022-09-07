using common.server;
using System;

namespace common.socks5
{
    public interface ISocks5ServerHandler
    {
        void InputData(IConnection connection);
    }
}
