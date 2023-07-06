using common.libs.extends;
using LiteNetLib;
using System.Buffers;
using System;
using System.Net;
using System.Threading.Tasks;

namespace common.server
{
    public interface IUdpServer : IServer
    {
        public void Start(int port, int timeout = 20000);
        public Task<IConnection> CreateConnection(IPEndPoint address);
        public NetPeer Connect(IPEndPoint address);

        public bool SendUnconnectedMessage(Memory<byte> message, IPEndPoint address);

    }
}
