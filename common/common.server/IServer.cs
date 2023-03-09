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

        /// <summary>
        /// 收到包
        /// </summary>
        public Func<IConnection, Task> OnPacket { get; set; }
        public Action<IPEndPoint, Memory<byte>> OnMessage { get; set; }
        /// <summary>
        /// 有人离线
        /// </summary>
        public Action<IConnection> OnDisconnect { get; set; }
        /// <summary>
        /// 有人上线
        /// </summary>
        public Action<IConnection> OnConnected { get; set; }
    }
}
