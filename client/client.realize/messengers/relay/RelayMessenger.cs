using client.messengers.clients;
using client.messengers.register;
using client.messengers.relay;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
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
        private readonly Config config;

        public RelayMessenger(IClientInfoCaching clientInfoCaching, MessengerSender messengerSender,
            RelayMessengerSender relayMessengerSender, IRelayValidator relayValidator,
            IConnecRouteCaching connecRouteCaching, RegisterStateInfo registerStateInfo, Config config)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.messengerSender = messengerSender;
            this.relayMessengerSender = relayMessengerSender;
            this.relayValidator = relayValidator;
            this.connecRouteCaching = connecRouteCaching;
            this.registerStateInfo = registerStateInfo;
            this.config = config;
        }

        [MessengerId((ushort)RelayMessengerIds.Relay)]
        public void Relay(IConnection connection)
        {
            if (relayValidator.Validate(connection) == false)
            {
                return;
            }

            RelayInfo relayInfo = new RelayInfo();
            relayInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            relayMessengerSender.OnRelay.Push(relayInfo);
        }

        [MessengerId((ushort)RelayMessengerIds.Delay)]
        public byte[] Delay(IConnection connection)
        {
            return Helper.TrueArray;
        }



        [MessengerId((ushort)RelayMessengerIds.AskConnects)]
        public void AskConnects(IConnection connection)
        {
            if (config.Client.UseRelay)
            {
                ulong fromid = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
                _ = relayMessengerSender.Connects(new ConnectsInfo
                {
                    Id = registerStateInfo.ConnectId,
                    ToId = fromid,
                    Connects = clientInfoCaching.All().Where(c => c.Connected && c.ConnectType != ClientConnectTypes.RelayServer).Select(c => c.Id).ToArray(),
                });
            }
        }
        [MessengerId((ushort)RelayMessengerIds.Connects)]
        public void Connects(IConnection connection)
        {
            ConnectsInfo connectInfo = new ConnectsInfo();
            connectInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            connecRouteCaching.AddConnects(connectInfo);
        }
    }
}
