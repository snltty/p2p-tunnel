using common.libs;
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
        private readonly NumberSpace numberSpaceTunnelName = new NumberSpace((ulong)TunnelDefaults.MAX + 1);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCache"></param>
        public ClientsMessenger(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ClientsMessengerIds.IP)]
        public byte[] Ip(IConnection connection)
        {
            return connection.Address.Address.GetAddressBytes();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ClientsMessengerIds.Port)]
        public void Port(IConnection connection)
        {
            connection.Write((ushort)connection.Address.Port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)ClientsMessengerIds.AddTunnel)]
        public void AddTunnel(IConnection connection)
        {
            TunnelRegisterInfo model = new TunnelRegisterInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                if (model.TunnelName == (ulong)TunnelDefaults.MIN)
                {
                    model.TunnelName = numberSpaceTunnelName.Increment();
                }

                source.AddTunnel(new TunnelRegisterCacheInfo
                {
                    IsDefault = true,
                    LocalPort = model.LocalPort,
                    Port = model.Port,
                    Servertype = connection.ServerType,
                    TunnelName = model.TunnelName,
                });
            }
            connection.Write(model.TunnelName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)ClientsMessengerIds.RemoveTunnel)]
        public void RemoveTunnel(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                ulong tunnelName = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
                source.RemoveTunnel(tunnelName);
            }
        }


    }
}
