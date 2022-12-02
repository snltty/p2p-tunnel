using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.clients
{
    public sealed class ClientsMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public ClientsMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public SimpleSubPushHandler<ClientsInfo> OnServerClientsData { get; } = new SimpleSubPushHandler<ClientsInfo>();

        public async Task<ushort> GetTunnelPort(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Payload = Helper.EmptyArray,
                MessengerId =(ushort)ClientsMessengerIds.Port,
                Timeout = 2000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.ToUInt16();
            }
            return 0;
        }
        public async Task<ulong> AddTunnel(IConnection connection, ulong tunnelName, ushort port, ushort localPort)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Payload = new TunnelRegisterInfo { LocalPort = localPort, Port = port, TunnelName = tunnelName }.ToBytes(),
                MessengerId = (ushort)ClientsMessengerIds.AddTunnel,
                Timeout = 2000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.ToUInt64();
            }
            return 0;
        }
        public async Task RemoveTunnel(IConnection connection, ulong tunnelName)
        {
            var resp = await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                Payload = tunnelName.ToBytes(),
                MessengerId = (ushort)ClientsMessengerIds.RemoveTunnel,
                Timeout = 2000
            }).ConfigureAwait(false);
        }
    }
}
