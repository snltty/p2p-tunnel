using client.messengers.clients;
using client.messengers.register;
using common.server;
using common.server.model;
using System;

namespace client.realize.messengers.clients
{
    public class DefaultRelayConnectionSelector : IRelayConnectionSelector
    {
        private readonly RegisterStateInfo registerState;
        public DefaultRelayConnectionSelector(RegisterStateInfo registerState)
        {
            this.registerState = registerState;
        }
        public IConnection Select(ServerType serverType)
        {
            return serverType switch
            {
                ServerType.TCP => registerState.TcpConnection,
                ServerType.UDP => registerState.UdpConnection,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
