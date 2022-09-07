using common.server;
using common.server.model;
using common.socks5;

namespace client.service.vea
{
    public interface IVeaSocks5MessengerSender : ISocks5MessengerSender
    {

    }

    public class VeaSocks5MessengerSender : Socks5MessengerSender, IVeaSocks5MessengerSender
    {
        protected override string Target { get; } = "veasocks5";
        public VeaSocks5MessengerSender(MessengerSender messengerSender) : base(messengerSender)
        {
        }
    }
}
