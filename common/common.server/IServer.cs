using common.libs;
using System;
using System.Net;
using System.Threading.Tasks;

namespace common.server
{
    public interface IServer
    {
        public void Start(int port);

        public void Stop();
        public void Disponse();

        public Task InputData(IConnection connection);

        public Func<IConnection, Task> OnPacket { get; set; }
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; }
        public Action<IConnection> OnConnected { get; set; }
    }
}
