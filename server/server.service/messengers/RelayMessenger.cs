using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System.Linq;

namespace server.service.messengers
{
    /// <summary>
    /// 中继
    /// </summary>
    [MessengerIdRange((ushort)RelayMessengerIds.Min, (ushort)RelayMessengerIds.Max)]
    public sealed class RelayMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCache"></param>
        /// <param name="messengerSender"></param>
        public RelayMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)RelayMessengerIds.Delay)]
        public byte[] Delay(IConnection connection)
        {
            return Helper.TrueArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)RelayMessengerIds.AskConnects)]
        public void AskConnects(IConnection connection)
        {
            foreach (var item in clientRegisterCache.Get().Where(c => c.Id != connection.ConnectId))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = item.OnLineConnection,
                    MessengerId = (ushort)RelayMessengerIds.AskConnects,
                    Payload = connection.ConnectId.ToBytes()
                });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
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
    /// <summary>
    /// 
    /// </summary>
    public class RelaySourceConnectionSelector : IRelaySourceConnectionSelector
    {
        private readonly IClientRegisterCaching clientRegisterCaching;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCaching"></param>
        public RelaySourceConnectionSelector(IClientRegisterCaching clientRegisterCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="relayid"></param>
        /// <returns></returns>
        public IConnection Select(IConnection connection, ulong relayid)
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
