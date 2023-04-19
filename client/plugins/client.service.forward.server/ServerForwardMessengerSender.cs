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
                MessengerId = (ushort)ServerForwardMessengerIds.Domains,
                Connection = Connection
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ServerForwardMessengerIds.Ports,
                Connection = Connection
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> SignOut(IConnection Connection, ServerForwardSignOutInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ServerForwardMessengerIds.SignOut,
                Connection = Connection,
                Payload = data.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> SignIn(IConnection Connection, ServerForwardSignInInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ServerForwardMessengerIds.SignIn,
                Connection = Connection,
                Payload = data.ToBytes(),
            }).ConfigureAwait(false);
        }

    }
}
