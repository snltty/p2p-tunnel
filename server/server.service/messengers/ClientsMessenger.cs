using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System;

namespace server.service.messengers
{
    public class ClientsMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly NumberSpace numberSpaceTunnelName = new NumberSpace((ulong)TunnelDefaults.MAX + 1);
        public ClientsMessenger(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public byte[] Ip(IConnection connection)
        {
            return connection.Address.Address.GetAddressBytes();
        }
        public byte[] Port(IConnection connection)
        {
            return connection.Address.Port.ToBytes();
        }

        public byte[] AddTunnel(IConnection connection)
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
            return model.TunnelName.ToBytes();
        }

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
