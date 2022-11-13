using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.realize.messengers.relay
{
    /// <summary>
    /// 中继
    /// </summary>

    [MessengerIdRange((ushort)RelayMessengerIds.Min, (ushort)RelayMessengerIds.Max)]
    public class RelayMessenger : IMessenger
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly MessengerSender messengerSender;
        private readonly RelayMessengerSender relayMessengerSender;
        private readonly IRelayValidator relayValidator;

        public RelayMessenger(IClientInfoCaching clientInfoCaching, MessengerSender messengerSender, RelayMessengerSender relayMessengerSender, IRelayValidator relayValidator)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.messengerSender = messengerSender;
            this.relayMessengerSender = relayMessengerSender;
            this.relayValidator = relayValidator;
        }

        [MessengerId((ushort)RelayMessengerIds.Notify)]
        public async Task Notify(IConnection connection)
        {
            if (relayValidator.Validate(connection) == false)
            {
                return;
            }

            RelayInfo relayInfo = new RelayInfo();
            relayInfo.DeBytes(connection.ReceiveRequestWrap.Payload);

            if (relayInfo.ToId == 0)
            {
                relayMessengerSender.OnRelay.Push(new OnRelayInfo { Connection = connection, FromId = relayInfo.FromId });
            }
            else
            {
                if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo source))
                {
                    if (clientInfoCaching.Get(relayInfo.ToId, out ClientInfo target))
                    {
                        connection.ReceiveRequestWrap.Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection;
                        RelayInfo.ClearToId(connection.ReceiveRequestWrap.Payload);
                        await messengerSender.SendOnly(connection.ReceiveRequestWrap).ConfigureAwait(false);
                    }
                }
            }
        }

        [MessengerId((ushort)RelayMessengerIds.Verify)]
        public byte[] Verify(IConnection connection)
        {
            if (relayValidator.Validate(connection) == false)
            {
                return Helper.FalseArray;
            }

            ulong toid = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
            if (clientInfoCaching.Get(toid, out ClientInfo source))
            {
                return connection.ServerType switch
                {
                    ServerType.TCP => source.TcpConnected && source.TcpConnectType != ClientConnectTypes.RelayServer ? Helper.TrueArray : Helper.FalseArray,
                    ServerType.UDP => source.UdpConnected && source.UdpConnectType != ClientConnectTypes.RelayServer ? Helper.TrueArray : Helper.FalseArray,
                    _ => Helper.FalseArray,
                };
            }

            return Helper.FalseArray;
        }

        [MessengerId((ushort)RelayMessengerIds.Delay)]
        public byte[] Delay(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}
