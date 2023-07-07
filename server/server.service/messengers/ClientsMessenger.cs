using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.singnin;
using System.Net;
using System;

namespace server.service.messengers
{
    /// <summary>
    /// 客户端
    /// </summary>
    [MessengerIdRange((ushort)ClientsMessengerIds.Min, (ushort)ClientsMessengerIds.Max)]
    public sealed class ClientsMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCache;
        private readonly ClientsMessenger clientsMessenger;
        public ClientsMessenger(IClientSignInCaching clientSignInCache, IUdpServer udpServer, MessengerResolver messenger)
        {
            this.clientSignInCache = clientSignInCache;

            if (messenger.GetMessenger((ushort)ClientsMessengerIds.AddTunnel, out object obj))
            {
                clientsMessenger = obj as ClientsMessenger;
            }
            udpServer.OnMessage += (IPEndPoint remoteEndpoint, Memory<byte> data) =>
            {
                try
                {
                    TunnelRegisterInfo model = new TunnelRegisterInfo();
                    model.DeBytes(data);
                    if (clientsMessenger != null)
                    {
                        clientsMessenger.AddTunnel(model, remoteEndpoint.Port);
                        udpServer.SendUnconnectedMessage(((ushort)remoteEndpoint.Port).ToBytes(), remoteEndpoint);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            };
        }

        [MessengerId((ushort)ClientsMessengerIds.IP)]
        public void Ip(IConnection connection)
        {
            connection.Write(connection.Address.Address.GetAddressBytes());
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
            if (clientSignInCache.Get(model.TargetId, out SignInCacheInfo target))
            {
                target.AddTunnel(new TunnelCacheInfo
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
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
            {
                ulong targetId = connection.ReceiveRequestWrap.Payload.Span.ToUInt64();
                source.RemoveTunnel(targetId);
            }
        }
    }
}
