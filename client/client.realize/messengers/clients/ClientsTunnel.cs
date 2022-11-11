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

        public Action<IConnection, IConnection> OnDisConnect { get; set; } = (IConnection connection, IConnection connection1) => { };

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
        public async Task<(ulong, ushort)> NewBind(ServerType serverType, ulong tunnelName)
        {
            IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
            while (true)
            {
                try
                {
                    ushort port = NetworkHelper.GetRandomPort(new System.Collections.Generic.List<ushort> { registerState.LocalInfo.UdpPort, registerState.LocalInfo.TcpPort });

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
        private async Task<ulong> NewBindUdp(ushort localport, IPAddress serverAddress, ulong tunnelName)
        {
            ValuePacket<ulong> model = new ValuePacket<ulong> { Value = tunnelName };

            IConnection connection = null;
            UdpServer tempUdpServer = new UdpServer();
            tempUdpServer.OnPacket = udpServer.InputData;

            tempUdpServer.OnDisconnect.Sub((IConnection _connection) =>
            {
                if (ReferenceEquals(connection, _connection) == false)
                {
                    clientInfoCaching.RemoveUdpserver(model.Value);
                    tempUdpServer.Disponse();

                    OnDisConnect(_connection, registerState.UdpConnection);
                }
            });
            tempUdpServer.Start(localport, config.Client.TimeoutDelay);
            tempUdpServer.SetSpeedLimit(config.Client.UdpUploadSpeedLimit);
            connection = await tempUdpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));
            while (connection == null)
            {
                connection = await tempUdpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));
            }

            ushort port = await clientsMessengerSender.GetTunnelPort(connection);
            model.Value = await clientsMessengerSender.AddTunnel(registerState.UdpConnection, model.Value, port, localport);

            clientInfoCaching.AddTunnelPort(model.Value, port);
            clientInfoCaching.AddUdpserver(model.Value, tempUdpServer);

            return model.Value;
        }

        /// <summary>
        /// 新绑定一个tcp
        /// </summary>
        /// <param name="localport"></param>
        /// <param name="serverAddress"></param>
        /// <param name="tunnelName"></param>
        /// <returns></returns>
        private async Task<ulong> NewBindTcp(ushort localport, IPAddress serverAddress, ulong tunnelName)
        {
            TcpServer tempTcpServer = new TcpServer();
            tempTcpServer.SetBufferSize(config.Client.TcpBufferSize);
            tempTcpServer.OnConnected1 = (socket) =>
            {
                tcpServer.BindReceive(socket, config.Client.TcpBufferSize);
                tempTcpServer.Disponse();
            };
            tempTcpServer.Start1(localport);

            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, localport);

            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                tcpSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            catch (Exception)
            {
            }
            tcpSocket.KeepAlive(time: config.Client.TimeoutDelay / 1000 / 5);
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));

            IPAddress localAddress = (tcpSocket.LocalEndPoint as IPEndPoint).Address;

            IConnection connection = tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);

            ushort port = await clientsMessengerSender.GetTunnelPort(connection);
            tunnelName = await clientsMessengerSender.AddTunnel(registerState.TcpConnection, tunnelName, port, localport);

            clientInfoCaching.AddTunnelPort(tunnelName, port);

            return tunnelName;
        }
    }
}
