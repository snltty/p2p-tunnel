using common.server;

namespace common.socks5
{
    public interface ISocks5ClientHandler
    {
        void InputData(IConnection connection);
        void Flush();
    }
}
