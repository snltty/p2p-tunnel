using common.server;
using System;

namespace common.socks5
{
    public class Socks5Messenger : IMessenger
    {
        private readonly ISocks5ClientHandler socks5ClientHandler;
        private readonly ISocks5ServerHandler socks5ServerHandler;
        public Socks5Messenger(ISocks5ClientHandler socks5ClientHandler, ISocks5ServerHandler socks5ServerHandler)
        {
            this.socks5ClientHandler = socks5ClientHandler;
            this.socks5ServerHandler = socks5ServerHandler;
        }

        public void Request(IConnection connection)
        {
            socks5ServerHandler.InputData(connection);
        }
        public void Response(IConnection connection)
        {
            socks5ClientHandler.InputData(connection);
        }
    }
}
