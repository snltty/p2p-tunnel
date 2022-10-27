using common.libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets.Kcp;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

namespace common.server.servers.kcp
{
    public class KcpServer : IUdpServer, IKcpCallback
    {
        public Func<IConnection, Task> OnPacket { get; set; } = (connection) => { return Task.CompletedTask; };

        public SimpleSubPushHandler<IConnection> OnDisconnect { get; } = new SimpleSubPushHandler<IConnection>();

        public Action<IConnection> OnConnected { get; set; } = (connection) => { };


        public void Start(int port, IPAddress ip = null)
        {
            Start(port, ip, 20000);
        }

        private Socket socket;
        private PoolSegManager.Kcp kcp;
        public void Start(int port, IPAddress ip = null, int timeout = 20000)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            kcp = new PoolSegManager.Kcp(2001, this);
            kcp.NoDelay(1, 1, 2, 1);//fast
            kcp.Interval(1);
            kcp.WndSize(1024, 1024);
        }
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            //socket.Send
        }


        public Task<IConnection> CreateConnection(IPEndPoint address)
        {
            return null;
        }
        public void Stop()
        {
        }
        public void Disponse()
        {
        }

        public async Task InputData(IConnection connection)
        {
            await OnPacket(connection);
        }


    }
}
