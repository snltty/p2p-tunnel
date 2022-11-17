using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace server.service.messengers
{
    /// <summary>
    /// 中继
    /// </summary>
    [MessengerIdRange((ushort)RelayMessengerIds.Min, (ushort)RelayMessengerIds.Max)]
    public class RelayMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;
        private readonly IRelayValidator relayValidator;

        public RelayMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender, IRelayValidator relayValidator)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
            this.relayValidator = relayValidator;
        }

        [MessengerId((ushort)RelayMessengerIds.Delay)]
        public byte[] Delay(IConnection connection)
        {
            return Helper.TrueArray;
        }


        [MessengerId((ushort)RelayMessengerIds.AskConnects)]
        public void AskConnects(IConnection connection)
        {
            foreach (var item in clientRegisterCache.GetAll().Where(c => c.Id != connection.ConnectId))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = item.OnLineConnection,
                    MessengerId = (ushort)RelayMessengerIds.AskConnects,
                    Payload = connection.ConnectId.ToBytes()
                });
            }
        }

        [MessengerId((ushort)RelayMessengerIds.Connects)]
        public void Connects(IConnection connection)
        {
            ulong toid = ConnectsInfo.ReadToId(connection.ReceiveRequestWrap.Payload);
            if (clientRegisterCache.Get(toid, out RegisterCacheInfo client))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = client.OnLineConnection,
                    MessengerId = (ushort)RelayMessengerIds.Connects,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }
        }
    }

    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        private readonly IClientRegisterCaching clientRegisterCaching;

        public SourceConnectionSelector(IClientRegisterCaching clientRegisterCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
        }

        public IConnection SelectSource(IConnection connection, ulong relayid)
        {
            if (relayid > 0)
            {
                if (clientRegisterCaching.Get(relayid, out RegisterCacheInfo client))
                {
                    return client.OnLineConnection;
                }
            }
            return connection;
        }

        public IConnection SelectTarget(IConnection connection, ulong relayid)
        {
            if (relayid > 0)
            {
                if (clientRegisterCaching.Get(relayid, out RegisterCacheInfo client))
                {
                    return client.OnLineConnection;
                }
            }
            return connection;
        }
    }
}
