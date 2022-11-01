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
        protected override int TargetRequest { get; } = (int)VeaSocks5MessengerIds.Request;
        protected override int TargetResponse { get; } = (int)VeaSocks5MessengerIds.Response;
        public VeaSocks5MessengerSender(MessengerSender messengerSender) : base(messengerSender)
        {
        }
    }
}
