using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.messengers
{
    /// <summary>
    /// 中继
    /// </summary>
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

        public async Task Execute(IConnection connection)
        {
            //A已注册
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                if (relayValidator.Validate(source.GroupId) == false)
                {
                    return;
                }

                //B已注册
                if (clientRegisterCache.Get(connection.ReceiveRequestWrap.RelayId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        connection.ReceiveRequestWrap.Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection;
                        connection.ReceiveRequestWrap.MemoryPath = connection.ReceiveRequestWrap.OriginPath;
                        connection.ReceiveRequestWrap.OriginPath = Helper.EmptyArray;
                        connection.ReceiveRequestWrap.RelayId = source.Id;
                        await messengerSender.SendOnly(connection.ReceiveRequestWrap).ConfigureAwait(false);
                    }
                }
            }
        }
        public async Task Relay(IConnection connection)
        {
            RelayInfo relayInfo = new RelayInfo();
            relayInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            if (relayInfo.ToId > 0)
            {
                if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
                {
                    if (relayValidator.Validate(source.GroupId) == false)
                    {
                        return;
                    }

                    if (clientRegisterCache.Get(relayInfo.ToId, out RegisterCacheInfo target))
                    {
                        if (source.GroupId == target.GroupId)
                        {
                            connection.ReceiveRequestWrap.Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection;
                            RelayInfo.ClearToId(connection.ReceiveRequestWrap.Payload);
                            await messengerSender.SendOnly(connection.ReceiveRequestWrap).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        public byte[] Verify(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                if (relayValidator.Validate(source.GroupId) == false)
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

        public byte[] Delay(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }

    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        private readonly IClientRegisterCaching clientRegisterCaching;

        public SourceConnectionSelector(IClientRegisterCaching clientRegisterCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
        }

        public IConnection Select(IConnection connection, ulong relayid)
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
