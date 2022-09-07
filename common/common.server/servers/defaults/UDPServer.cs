using common.libs;
using common.libs.extends;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace common.server.servers.defaults
{
    public class UDPServer : IUdpServer
    {
        public UDPServer()
        {
        }

        private UdpClient UdpClient { get; set; } = null;
        public SimpleSubPushHandler<IConnection> OnPacket { get; } = new SimpleSubPushHandler<IConnection>();
        public SimpleSubPushHandler<IConnection> OnDisconnect => new SimpleSubPushHandler<IConnection>();
        public Action<IConnection> OnConnected { get; set; } = (IConnection connection) => { };

        private ConcurrentDictionary<int, IConnection> connections = new ConcurrentDictionary<int, IConnection>();


        public void Start(int port, IPAddress ip = null)
        {
            if (UdpClient != null)
            {
                return;
            }

            UdpClient = new UdpClient(new IPEndPoint(ip ?? IPAddress.Any, port));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const uint IOC_IN = 0x80000000;
                int IOC_VENDOR = 0x18000000;
                int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                UdpClient.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }

            IAsyncResult result = UdpClient.BeginReceive(ProcessReceive, UdpClient);
            if (result.CompletedSynchronously)
            {
                ProcessReceive(result);
            }
        }
        private void ProcessReceive(IAsyncResult result)
        {
            try
            {
                if (UdpClient != null)
                {
                    IPEndPoint remoteEP = null;
                    byte[] bytes = UdpClient.EndReceive(result, ref remoteEP);

                    int hascode = remoteEP.GetHashCode();
                    if (!connections.TryGetValue(hascode, out IConnection connection))
                    {
                        connection = CreateConnectionIntenal(remoteEP);
                        connections.TryAdd(hascode, connection);
                    }

                    if (bytes.Length > 0)
                    {
                        connection.ReceiveData = bytes;
                        OnPacket.Push(connection);
                    }

                    if (UdpClient != null)
                    {
                        result = UdpClient.BeginReceive(ProcessReceive, connection);
                        if (result.CompletedSynchronously)
                        {
                            ProcessReceive(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                Stop();
            }
        }

        public void Stop()
        {
            if (UdpClient != null)
            {
                UdpClient.Close();
                UdpClient.Dispose();
                UdpClient = null;
            }
            foreach (var item in connections.Values)
            {
                item.Disponse();
            }
            connections.Clear();
        }

        public async Task<IConnection> CreateConnection(IPEndPoint address)
        {
            await Task.CompletedTask;
            return new UdpConnection(UdpClient, address);
        }

        private IConnection CreateConnectionIntenal(IPEndPoint address)
        {
            return new UdpConnection(UdpClient, address);
        }
    }

    public class UdpAsyncUserToken
    {
        public IConnection Connection { get; set; }
    }
}
