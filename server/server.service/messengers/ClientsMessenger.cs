using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;

namespace server.service.messengers
{
    public class ClientsMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly NumberSpace numberSpaceTunnelName = new NumberSpace((ulong)TunnelDefaults.TCP + 1);
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
            model.DeBytes(connection.ReceiveRequestWrap.Memory);
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                if (model.TunnelName == 0)
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
    }
}
