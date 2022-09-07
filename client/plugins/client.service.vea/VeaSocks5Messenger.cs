using common.server;
using common.socks5;

namespace client.service.vea
{
    public class VeaSocks5Messenger : Socks5Messenger
    {
        public VeaSocks5Messenger(IVeaSocks5ClientHandler socks5ClientHandler, IVeaSocks5ServerHandler socks5ServerHandler)
            : base(socks5ClientHandler, socks5ServerHandler)
        {

        }

        public new void Request(IConnection connection)
        {
            base.Request(connection);
        }
        public new void Response(IConnection connection)
        {
            base.Response(connection);
        }
    }
}
