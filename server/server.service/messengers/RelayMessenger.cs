using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.singnin;
using System.Linq;

namespace server.service.messengers
{
    /// <summary>
    /// 中继
    /// </summary>
    [MessengerIdRange((ushort)RelayMessengerIds.Min, (ushort)RelayMessengerIds.Max)]
    public sealed class RelayMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCache;
        private readonly MessengerSender messengerSender;

        public RelayMessenger(IClientSignInCaching clientSignInCache, MessengerSender messengerSender)
        {
            this.clientSignInCache = clientSignInCache;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)RelayMessengerIds.Delay)]
        public void Delay(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)RelayMessengerIds.AskConnects)]
        public void AskConnects(IConnection connection)
        {
            if(clientSignInCache.Get(connection.ConnectId,out SignInCacheInfo cache))
            {
                foreach (var item in clientSignInCache.Get(cache.GroupId).Where(c => c.ConnectionId != connection.ConnectId))
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)RelayMessengerIds.AskConnects,
                        Payload = connection.ConnectId.ToBytes()
                    });
                }
            }
        }

        [MessengerId((ushort)RelayMessengerIds.Connects)]
        public void Connects(IConnection connection)
        {
            ulong toid = ConnectsInfo.ReadToId(connection.ReceiveRequestWrap.Payload);
            if (clientSignInCache.Get(toid, out SignInCacheInfo client))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = client.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Connects,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }
        }
    }

    public sealed class RelaySourceConnectionSelector : IRelaySourceConnectionSelector
    {
        private readonly IClientSignInCaching clientSignInCaching;

        public RelaySourceConnectionSelector(IClientSignInCaching clientSignInCaching)
        {
            this.clientSignInCaching = clientSignInCaching;
        }

        public IConnection Select(IConnection connection, ulong relayid)
        {
            if (relayid > 0)
            {
                if (clientSignInCaching.Get(relayid, out SignInCacheInfo client))
                {
                    return client.Connection;
                }
            }
            return connection;
        }
    }
}
