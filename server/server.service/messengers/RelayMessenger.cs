using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
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

        [MessengerId((ushort)RelayMessengerIds.Notify)]
        public async Task Notify(IConnection connection)
        {
            RelayInfo relayInfo = new RelayInfo();
            relayInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            if (relayInfo.ToId > 0)
            {
                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    if (relayValidator.Validate(connection) == false)
                    {
                        return;
                    }

                    if (clientRegisterCache.Get(relayInfo.ToId, out RegisterCacheInfo target))
                    {
                        if (source.GroupId == target.GroupId)
                        {
                            IConnection online = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection;
                            if (online == null || online.Connected == false)
                            {
                                online = target.OnLineConnection;
                            }

                            connection.ReceiveRequestWrap.Connection = online;
                            RelayInfo.ClearToId(connection.ReceiveRequestWrap.Payload);
                            await messengerSender.SendOnly(connection.ReceiveRequestWrap).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        [MessengerId((ushort)RelayMessengerIds.Verify)]
        public byte[] Verify(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                if (relayValidator.Validate(connection) == false)
                {
                    return Helper.FalseArray;
                }

                ulong toid = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
                if (clientRegisterCache.Get(toid, out RegisterCacheInfo target))
                {
                    if (source.GroupId == target.GroupId)
                    {
                        return connection.ServerType switch
                        {
                            ServerType.TCP => source.TcpConnection != null && source.TcpConnection.Connected ? Helper.TrueArray : Helper.FalseArray,
                            ServerType.UDP => source.UdpConnection != null && source.UdpConnection.Connected ? Helper.TrueArray : Helper.FalseArray,
                            _ => Helper.FalseArray,
                        };
                    }
                }
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

        [MessengerId((ushort)RelayMessengerIds.Routes)]
        public void Routes(IConnection connection)
        {
            ulong toid = RoutesInfo.ReadToId(connection.ReceiveRequestWrap.Payload);
            if (clientRegisterCache.Get(toid, out RegisterCacheInfo client))
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = client.OnLineConnection,
                    MessengerId = (ushort)RelayMessengerIds.Routes,
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
                    return connection.ServerType == ServerType.TCP ? client.TcpConnection : client.UdpConnection;
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
                    return connection.ServerType == ServerType.TCP ? client.TcpConnection : client.UdpConnection;
                }
            }
            return connection;
        }
    }
}
