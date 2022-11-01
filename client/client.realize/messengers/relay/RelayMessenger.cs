using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.relay
{
    /// <summary>
    /// 中继
    /// </summary>

    [MessengerIdRange((int)RelayMessengerIds.Min,(int)RelayMessengerIds.Max)]
    public class RelayMessenger : IMessenger
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly MessengerSender messengerSender;
        private readonly Config config;
        private readonly RelayMessengerSender relayMessengerSender;

        public RelayMessenger(IClientInfoCaching clientInfoCaching, MessengerSender messengerSender, Config config, RelayMessengerSender relayMessengerSender)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.messengerSender = messengerSender;
            this.config = config;
            this.relayMessengerSender = relayMessengerSender;
        }

        [MessengerId((int)RelayMessengerIds.Relay)]
        public async Task Relay(IConnection connection)
        {
            if (config.Client.UseRelay == false)
            {
                return;
            }

            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo source))
            {
                if (clientInfoCaching.Get(connection.ReceiveRequestWrap.RelayId, out ClientInfo target))
                {
                    connection.ReceiveRequestWrap.Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection;
                    connection.ReceiveRequestWrap.MessengerId = connection.ReceiveRequestWrap.OriginMessengerId;
                    connection.ReceiveRequestWrap.OriginMessengerId = 0;
                    connection.ReceiveRequestWrap.RelayId = source.Id;
                    await messengerSender.SendOnly(connection.ReceiveRequestWrap).ConfigureAwait(false);
                }
            }
        }

        [MessengerId((int)RelayMessengerIds.Notify)]
        public async Task Notify(IConnection connection)
        {
            if (config.Client.UseRelay == false)
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

        [MessengerId((int)RelayMessengerIds.Verify)]
        public byte[] Verify(IConnection connection)
        {
            if (config.Client.UseRelay == false)
            {
                return Helper.FalseArray;
            }

            ulong toid = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
            if (clientInfoCaching.Get(toid, out ClientInfo source))
            {
                return connection.ServerType switch
                {
                    ServerType.TCP => source.TcpConnected && source.TcpConnectType == ClientConnectTypes.P2P ? Helper.TrueArray : Helper.FalseArray,
                    ServerType.UDP => source.UdpConnected && source.UdpConnectType == ClientConnectTypes.P2P ? Helper.TrueArray : Helper.FalseArray,
                    _ => Helper.FalseArray,
                };
            }

            return Helper.FalseArray;
        }

        [MessengerId((int)RelayMessengerIds.Delay)]
        public byte[] Delay(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}
