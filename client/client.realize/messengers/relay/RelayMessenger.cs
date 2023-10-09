using client.messengers.clients;
using client.messengers.signin;
using client.messengers.relay;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Linq;

namespace client.realize.messengers.relay
{
    /// <summary>
    /// 中继
    /// </summary>
    [MessengerIdRange((ushort)RelayMessengerIds.Min, (ushort)RelayMessengerIds.Max)]
    public sealed class RelayMessenger : IMessenger
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly RelayMessengerSender relayMessengerSender;
        private readonly IRelayValidator relayValidator;
        private readonly IClientConnectsCaching connecRouteCaching;
        private readonly SignInStateInfo signInStateInfo;
        private readonly Config config;

        public RelayMessenger(IClientInfoCaching clientInfoCaching,
            RelayMessengerSender relayMessengerSender, IRelayValidator relayValidator,
            IClientConnectsCaching connecRouteCaching, SignInStateInfo signInStateInfo, Config config)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.relayMessengerSender = relayMessengerSender;
            this.relayValidator = relayValidator;
            this.connecRouteCaching = connecRouteCaching;
            this.signInStateInfo = signInStateInfo;
            this.config = config;
        }

        [MessengerId((ushort)RelayMessengerIds.Relay)]
        public void Relay(IConnection connection)
        {
            if (relayValidator.Validate(connection) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            RelayInfo relayInfo = new RelayInfo();
            relayInfo.Connection = connection;
            relayInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            relayMessengerSender.OnRelay?.Invoke(relayInfo);

            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)RelayMessengerIds.Delay)]
        public void Delay(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)RelayMessengerIds.AskConnects)]
        public void AskConnects(IConnection connection)
        {
            if (config.Client.UseRelay)
            {
                ulong fromid = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
                _ = relayMessengerSender.Connects(new ConnectsInfo
                {
                    Id = signInStateInfo.ConnectId,
                    ToId = fromid,
                    Connects = clientInfoCaching.All().Where(c => c.Connected && c.ConnectType == ClientConnectTypes.P2P && relayValidator.Validate(connection)).Select(c => c.ConnectionId).ToArray(),
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
