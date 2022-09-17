using client.messengers.clients;
using common.libs;
using common.server.model;
using common.server.servers.iocp;
using common.server.servers.rudp;
using common.server;
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using client.messengers.register;
using common.libs.extends;

namespace client.realize.messengers.clients
{
    public class ClientsTunnel : IClientsTunnel
    {
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly Config config;
        private readonly IUdpServer udpServer;
        private readonly ITcpServer tcpServer;
        private readonly ClientsMessengerSender clientsMessengerSender;
        public ClientsTunnel(ClientsMessengerSender clientsMessengerSender, IClientInfoCaching clientInfoCaching, RegisterStateInfo registerState, Config config, IUdpServer udpServer, ITcpServer tcpServer
        )
        {
            this.clientsMessengerSender = clientsMessengerSender;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
            this.udpServer = udpServer;
            this.tcpServer = tcpServer;
        }
        /// <summary>
        /// 新绑定
        /// </summary>
        /// <param name="serverType"></param>
        /// <param name="tunnelName"></param>
        /// <returns></returns>
        public async Task<(ulong, int)> NewBind(ServerType serverType, ulong tunnelName)
        {
            IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
            while (true)
            {
                try
                {
                    int port = NetworkHelper.GetRandomPort();

                    return serverType switch
                    {
                        ServerType.TCP => (await NewBindTcp(port, serverAddress, tunnelName), port),
                        ServerType.UDP => (await NewBindUdp(port, serverAddress, tunnelName), port),
                        _ => (0, port),
                    };
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            }
        }
        /// <summary>
        /// 新绑定一个udp
        /// </summary>
        /// <param name="localport"></param>
        /// <param name="serverAddress"></param>
        /// <param name="tunnelName"></param>
        /// <returns></returns>
        private async Task<ulong> NewBindUdp(int localport, IPAddress serverAddress, ulong tunnelName)
        {
            IConnection connection = null;
            UdpServer tempUdpServer = new UdpServer();
            tempUdpServer.OnPacket.Sub(udpServer.InputData);
            tempUdpServer.OnDisconnect.Sub((IConnection _connection) => { if (connection != _connection) tempUdpServer.Disponse(); });
            tempUdpServer.Start(localport, config.Client.BindIp, config.Client.TimeoutDelay);
            connection = await tempUdpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));

            int port = await clientsMessengerSender.GetTunnelPort(connection);
            tunnelName = await clientsMessengerSender.AddTunnel(registerState.UdpConnection, tunnelName, port, localport);

            clientInfoCaching.AddTunnelPort(tunnelName, port);
            clientInfoCaching.AddUdpserver(tunnelName, tempUdpServer);

            return tunnelName;
        }
        /// <summary>
        /// 新绑定一个tcp
        /// </summary>
        /// <param name="localport"></param>
        /// <param name="serverAddress"></param>
        /// <param name="tunnelName"></param>
        /// <returns></returns>
        private async Task<ulong> NewBindTcp(int localport, IPAddress serverAddress, ulong tunnelName)
        {
            TcpServer tempTcpServer = new TcpServer();
            tempTcpServer.SetBufferSize(config.Client.TcpBufferSize);
            tempTcpServer.OnPacket.Sub(tcpServer.InputData);
            tempTcpServer.OnDisconnect.Sub((IConnection connection) => tempTcpServer.Disponse());
            tempTcpServer.Start(localport, config.Client.BindIp);

            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, localport);

            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive(time: config.Client.TimeoutDelay / 5 / 1000);
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));

            IPAddress localAddress = (tcpSocket.LocalEndPoint as IPEndPoint).Address;

            IConnection connection = tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);

            int port = await clientsMessengerSender.GetTunnelPort(connection);
            tunnelName = await clientsMessengerSender.AddTunnel(registerState.TcpConnection, tunnelName, port, localport);

            clientInfoCaching.AddTunnelPort(tunnelName, port);

            return tunnelName;
        }
    }
}
