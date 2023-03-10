using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;

namespace server.service.messengers
{
    /// <summary>
    /// 客户端
    /// </summary>
    [MessengerIdRange((ushort)ClientsMessengerIds.Min, (ushort)ClientsMessengerIds.Max)]
    public sealed class ClientsMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public ClientsMessenger(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        [MessengerId((ushort)ClientsMessengerIds.IP)]
        public byte[] Ip(IConnection connection)
        {
            return connection.Address.Address.GetAddressBytes();
        }

        [MessengerId((ushort)ClientsMessengerIds.Port)]
        public void Port(IConnection connection)
        {
            connection.Write((ushort)connection.Address.Port);
        }

        [MessengerId((ushort)ClientsMessengerIds.AddTunnel)]
        public void AddTunnel(IConnection connection)
        {
            TunnelRegisterInfo model = new TunnelRegisterInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            AddTunnel(model, connection.Address.Port);
            connection.Write((ushort)connection.Address.Port);
        }
        public void AddTunnel(TunnelRegisterInfo model, int port)
        {
            if (clientRegisterCache.Get(model.TargetId, out RegisterCacheInfo target))
            {
                target.AddTunnel(new TunnelRegisterCacheInfo
                {
                    LocalPort = model.LocalPort,
                    Port = port,
                    SourceId = model.SelfId
                });
            }
        }

        [MessengerId((ushort)ClientsMessengerIds.RemoveTunnel)]
        public void RemoveTunnel(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                ulong targetId = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
                source.RemoveTunnel(targetId);
            }
        }
    }
}
