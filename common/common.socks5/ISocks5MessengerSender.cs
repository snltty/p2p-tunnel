using common.server;

namespace common.socks5
{
    public interface ISocks5MessengerSender
    {
        public bool Request(Socks5Info data, IConnection connection);
        public void Response(Socks5Info data, IConnection connection);
        public void ResponseClose(ulong id, IConnection connection);
        public void RequestClose(ulong id, IConnection connection);
    }
}
