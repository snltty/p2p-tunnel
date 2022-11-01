using common.libs;
using common.server;
using common.server.model;

namespace common.socks5
{
    public class Socks5MessengerSender : ISocks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        protected virtual int TargetRequest { get; } = (int)Socks5MessengerIds.Request;
        protected virtual int TargetResponse { get; } = (int)Socks5MessengerIds.Response;

        public Socks5MessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public bool Request(Socks5Info data, IConnection connection)
        {
            var bytes = data.ToBytes();
            return messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = TargetRequest,
                Connection = connection,
                Payload = bytes
            }).Result;
        }
        public void Response(Socks5Info data, IConnection connection)
        {
            var bytes = data.ToBytes();
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = TargetResponse,
                Connection = connection,
                Payload = bytes
            });
        }
        public void ResponseClose(ulong id, IConnection connection)
        {
            Response(new Socks5Info { Id = id, Data = Helper.EmptyArray, Socks5Step = Socks5EnumStep.Forward }, connection);
        }
        public void RequestClose(ulong id, IConnection connection)
        {
            Request(new Socks5Info { Id = id, Data = Helper.EmptyArray, Socks5Step = Socks5EnumStep.Forward }, connection);
        }
    }
}
