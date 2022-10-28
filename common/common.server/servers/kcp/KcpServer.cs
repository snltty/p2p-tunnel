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
            if (socket == null)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                kcp = new PoolSegManager.Kcp(2001, this);
                kcp.NoDelay(1, 1, 2, 1);//fast
                kcp.Interval(1);
                kcp.WndSize(1024, 1024);

                byte[] buffer = new byte[8 * 1024];
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Callback, new KcpUserToken
                {
                    Socket = socket,
                    Buffer = buffer
                });
            }
        }
        private void Callback(IAsyncResult result)
        {
            KcpUserToken state = result.AsyncState as KcpUserToken;
            int len = state.Socket.EndReceive(result);

            kcp.Input(state.Buffer.AsSpan(0, len));

            (IMemoryOwner<byte> buffer, int length) = kcp.TryRecv();
            if (length > 0)
            {
               // OnPacket();
            }

            state.Socket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, Callback, state);
        }
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            if (socket != null)
            {
                socket.Send(buffer.Memory.Span.Slice(0, avalidLength), SocketFlags.None);
            }
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

    class KcpUserToken
    {
        public Socket Socket { get; set; }
        public byte[] Buffer { get; set; }
    }

    class KcpPeer
    {
        private IPEndPoint ep;
        public KcpPeer(IPEndPoint ep)
        {

        }

    }

    enum MessageType : byte
    {
        Connect = 1,
        ConnectResponse = 2,
        DisConnect = 4,
        DisconnectResponse = 8,
    }
}
