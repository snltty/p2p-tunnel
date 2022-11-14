using client.messengers.clients;
using client.messengers.register;
using client.messengers.relay;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Linq;
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
        private readonly IConnecRouteCaching connecRouteCaching;
        private readonly RegisterStateInfo registerStateInfo;

        public RelayMessenger(IClientInfoCaching clientInfoCaching, MessengerSender messengerSender,
            RelayMessengerSender relayMessengerSender, IRelayValidator relayValidator,
            IConnecRouteCaching connecRouteCaching, RegisterStateInfo registerStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.messengerSender = messengerSender;
            this.relayMessengerSender = relayMessengerSender;
            this.relayValidator = relayValidator;
            this.connecRouteCaching = connecRouteCaching;
            this.registerStateInfo = registerStateInfo;
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
            if (clientInfoCaching.Get(toid, out ClientInfo target))
            {

                bool res = connection.ServerType switch
                {
                    ServerType.TCP => target.TcpConnected && target.TcpConnectType != ClientConnectTypes.RelayServer,
                    ServerType.UDP => target.UdpConnected && target.UdpConnectType != ClientConnectTypes.RelayServer,
                    _ => false,
                };
                return res ? Helper.TrueArray : Helper.FalseArray;
            }

            return Helper.FalseArray;
        }

        [MessengerId((ushort)RelayMessengerIds.Delay)]
        public byte[] Delay(IConnection connection)
        {
            return Helper.TrueArray;
        }



        [MessengerId((ushort)RelayMessengerIds.AskConnects)]
        public void AskConnects(IConnection connection)
        {
            ulong fromid = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
            _ = relayMessengerSender.Connects(new ConnectsInfo
            {
                Id = registerStateInfo.ConnectId,
                ToId = fromid,
                Connects = clientInfoCaching.All().Where(c => c.TcpConnected || c.UdpConnected).Select(c => new ConnectInfo
                {
                    Id = c.Id,
                    Tcp = c.TcpConnected,
                    Udp = c.UdpConnected
                }).ToArray(),
            });
        }
        [MessengerId((ushort)RelayMessengerIds.Connects)]
        public void Connects(IConnection connection)
        {
            ConnectsInfo connectInfo = new ConnectsInfo();
            connectInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            connecRouteCaching.AddConnects(connectInfo);
        }

        [MessengerId((ushort)RelayMessengerIds.Routes)]
        public void Routes(IConnection connection)
        {
            RoutesInfo routes = new RoutesInfo();
            routes.DeBytes(connection.ReceiveRequestWrap.Payload);
            connecRouteCaching.AddRoutes(routes);
        }
    }
}
