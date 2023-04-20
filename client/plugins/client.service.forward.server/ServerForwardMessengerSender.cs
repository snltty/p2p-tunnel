using common.forward;
using common.libs;
using common.server;
using common.server.model;
using server.service.forward.model;
using System;
using System.Threading.Tasks;

namespace client.service.forward.server
{
    public sealed class ServerForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public ServerForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public async Task<MessageResponeInfo> GetDomains(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ForwardMessengerIds.Domains,
                Connection = Connection
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ForwardMessengerIds.Ports,
                Connection = Connection
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> SignOut(IConnection Connection, ForwardSignOutInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ForwardMessengerIds.SignOut,
                Connection = Connection,
                Payload = data.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> SignIn(IConnection Connection, ForwardSignInInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ForwardMessengerIds.SignIn,
                Connection = Connection,
                Payload = data.ToBytes(),
            }).ConfigureAwait(false);
        }

    }
}
