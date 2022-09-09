using common.libs;
using System;
using System.Net;

namespace common.server
{
    public interface IServer
    {
        public void Start(int port, IPAddress ip = null);

        public void Stop();
        public void Disponse();

        public void InputData(IConnection connection);

        public SimpleSubPushHandler<IConnection> OnPacket { get; }
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; }
        public Action<IConnection> OnConnected { get; set; }
    }
}
