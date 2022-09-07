using common.server;
using common.socks5;

namespace server.service.socks5
{
    public class Socks5ClientHandler : ISocks5ClientHandler
    {
        public Socks5ClientHandler()
        {
        }

        public void Flush()
        {
        }

        public void InputData(IConnection connection)
        {
            throw new System.NotImplementedException();
        }
    }
}
