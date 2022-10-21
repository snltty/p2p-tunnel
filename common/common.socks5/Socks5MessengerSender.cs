using common.libs;
using common.server;
using common.server.model;

namespace common.socks5
{
    public class Socks5MessengerSender : ISocks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        protected virtual string Target { get; } = "socks5";

        public Socks5MessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public bool Request(Socks5Info data, IConnection connection)
        {
            var bytes = data.ToBytes();
            return messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = $"{Target}/request",
                Connection = connection,
                Payload = bytes
            }).Result;
        }
        public void Response(Socks5Info data, IConnection connection)
        {
            var bytes = data.ToBytes();
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = $"{Target}/response",
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
