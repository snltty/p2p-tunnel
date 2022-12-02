using common.server;
using common.server.model;
using common.socks5;

namespace client.service.vea
{
    public interface IVeaSocks5MessengerSender : ISocks5MessengerSender
    {

    }

    public sealed class VeaSocks5MessengerSender : Socks5MessengerSender, IVeaSocks5MessengerSender
    {
        protected override ushort TargetRequest { get; } = (ushort)VeaSocks5MessengerIds.Request;
        protected override ushort TargetResponse { get; } = (ushort)VeaSocks5MessengerIds.Response;
        public VeaSocks5MessengerSender(MessengerSender messengerSender) : base(messengerSender)
        {
        }
    }
}
