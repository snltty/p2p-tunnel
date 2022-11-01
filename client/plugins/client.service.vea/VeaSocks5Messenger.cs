using common.server;
using common.socks5;
using System;

namespace client.service.vea
{
    [MessengerIdRange((int)VeaSocks5MessengerIds.Min,(int)VeaSocks5MessengerIds.Max)]
    public class VeaSocks5Messenger : Socks5Messenger
    {
        public VeaSocks5Messenger(IVeaSocks5ClientHandler socks5ClientHandler, IVeaSocks5ServerHandler socks5ServerHandler)
            : base(socks5ClientHandler, socks5ServerHandler)
        {

        }

        [MessengerId((int)VeaSocks5MessengerIds.Request)]
        public new void Request(IConnection connection)
        {
            base.Request(connection);
        }

        [MessengerId((int)VeaSocks5MessengerIds.Response)]
        public new void Response(IConnection connection)
        {
            base.Response(connection);
        }
    }

    [Flags, MessengerIdEnum]
    public enum VeaSocks5MessengerIds : int
    {
        Min = 901,
        Request = 902,
        Response = 903,
        Ip = 904,
        Reset = 905,
        Max = 1000,
    }
}
