using common.libs;
using common.libs.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace common.proxy
{
    public interface IProxyServer
    {
        public void SetBufferSize(EnumProxyBufferSize buffersize);
        public void Start(ushort port, byte plugin);
        public void Stop(ushort port);
        public void Stop();
        public Task InputData(ProxyInfo info);
    }


    public sealed class ProxyServer : IProxyServer
    {
        private readonly ServersManager serversManager = new ServersManager();
        private readonly ClientsManager clientsManager = new ClientsManager();
        private NumberSpaceUInt32 requestIdNs = new NumberSpaceUInt32(0);
        SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        private EnumProxyBufferSize receiveBufferSize = EnumProxyBufferSize.KB_8;
        private readonly IProxyMessengerSender proxyMessengerSender;

        public ProxyServer(IProxyMessengerSender proxyMessengerSender)
        {
            this.proxyMessengerSender = proxyMessengerSender;
        }

        public void SetBufferSize(EnumProxyBufferSize buffersize)
        {
            receiveBufferSize = buffersize;
        }

        public void Start(ushort port, byte pluginId)
        {
            if (serversManager.Contains(port))
            {
                Logger.Instance.Error($"port:{port} already exists");
                return;
            }
            if (ProxyPluginLoader.GetPlugin(pluginId, out IProxyPlugin plugin) == false)
            {
                Logger.Instance.Error($"plugin:{pluginId} not exists");
                return;
            }

            BindAccept(port, plugin);
        }

        private void BindAccept(ushort port, IProxyPlugin plugin)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endpoint);
            socket.Listen(int.MaxValue);

            ServerInfo server = new ServerInfo
            {
                SourcePort = port,
                Socket = socket,
                UdpClient = new UdpClient(endpoint),
                UdpInfo = new ProxyInfo
                {
                    ProxyPlugin = plugin,
                }
            };
            server.UdpClient.EnableBroadcast = true;
            server.UdpClient.Client.WindowsUdpBug();
            serversManager.TryAdd(server);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            ProxyUserToken token = new ProxyUserToken
            {
                SourceSocket = socket,
                SourcePort = port,
                ProxyPlugin = plugin,
                UdpClient = server.UdpClient
            };
            acceptEventArg.UserToken = token;
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            IAsyncResult result = server.UdpClient.BeginReceive(ProcessReceiveUdp, server);
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            try
            {
                acceptEventArg.AcceptSocket = null;
                ProxyUserToken token = ((ProxyUserToken)acceptEventArg.UserToken);
                if (token.SourceSocket.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
            }
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    Logger.Instance.Error(e.LastOperation.ToString());
                    break;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            BindReceive(e);
            StartAccept(e);
        }
        private void BindReceive(SocketAsyncEventArgs e)
        {
            try
            {
                ProxyUserToken acceptToken = (e.UserToken as ProxyUserToken);

                uint id = requestIdNs.Increment();
                ProxyUserToken token = new ProxyUserToken
                {
                    SourceSocket = e.AcceptSocket,
                    Request = new ProxyInfo
                    {
                        RequestId = id,
                        BufferSize = receiveBufferSize,
                        Command = EnumProxyCommand.Connect,
                        AddressType = EnumProxyAddressType.IPV4,
                        ListenPort = acceptToken.SourcePort,
                        Step = EnumProxyStep.Command,
                        PluginId = acceptToken.ProxyPlugin.Id,
                        ProxyPlugin = acceptToken.ProxyPlugin
                    },
                    SourcePort = acceptToken.SourcePort,
                    ProxyPlugin = acceptToken.ProxyPlugin,
                    UdpClient = acceptToken.UdpClient,
                };
                clientsManager.TryAdd(token);

                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None
                };
                token.Saea = readEventArgs;

                token.SourceSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
                token.SourceSocket.SendTimeout = 5000;
                token.PoolBuffer = new byte[(byte)receiveBufferSize * 1024];
                readEventArgs.SetBuffer(token.PoolBuffer, 0, (byte)receiveBufferSize * 1024);
                readEventArgs.Completed += IO_Completed;

                if (token.SourceSocket.ReceiveAsync(readEventArgs) == false)
                {
                    ProcessReceive(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
            }
        }
        private async void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                ProxyUserToken token = (ProxyUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int totalLength = e.BytesTransferred;
                    Memory<byte> data = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    //有些客户端，会把一个包拆开发送，很奇怪，不得不验证一下数据完整性
                    if (token.Request.Step == EnumProxyStep.Command)
                    {
                        EnumProxyValidateDataResult validate = token.Request.ProxyPlugin.ValidateData(data);
                        if ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
                        {
                            //太短
                            while ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
                            {
                                totalLength += await token.SourceSocket.ReceiveAsync(e.Buffer.AsMemory(e.Offset + totalLength), SocketFlags.None);
                                data = e.Buffer.AsMemory(e.Offset, totalLength);
                                validate = token.Request.ProxyPlugin.ValidateData(data);
                            }
                        }

                        //不短，又不相等，直接关闭连接
                        if ((validate & EnumProxyValidateDataResult.Equal) != EnumProxyValidateDataResult.Equal)
                        {
                            CloseClientSocket(e);
                            return;
                        }
                    }

                    await Receive(token, data);
                    if (token.SourceSocket.Available > 0)
                    {
                        while (token.SourceSocket.Available > 0)
                        {
                            int length = await token.SourceSocket.ReceiveAsync(e.Buffer.AsMemory(), SocketFlags.None);
                            if (length > 0)
                            {
                                await Receive(token, e.Buffer.AsMemory(0, length));
                            }
                        }
                    }
                    if (token.SourceSocket.Connected)
                    {
                        if (token.SourceSocket.ReceiveAsync(e) == false)
                        {
                            ProcessReceive(e);
                        }
                    }
                    else
                    {
                        CloseClientSocket(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                CloseClientSocket(e);
                Logger.Instance.DebugError(ex);
            }
        }

        private async void ProcessReceiveUdp(IAsyncResult result)
        {
            IPEndPoint rep = null;
            try
            {
                ServerInfo server = result.AsyncState as ServerInfo;

                server.UdpInfo.Data = server.UdpClient.EndReceive(result, ref rep);
                server.UdpInfo.SourceEP = rep;
                await Receive(server.UdpInfo);
                server.UdpInfo.Data = Helper.EmptyArray;

                result = server.UdpClient.BeginReceive(ProcessReceiveUdp, server);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"socks5 listen udp -> error " + ex);
            }
        }

        private async Task Receive(ProxyUserToken token, Memory<byte> data)
        {
            await Receive(token.Request, data);
        }
        private async Task Receive(ProxyInfo info, Memory<byte> data)
        {
            info.Data = data;
            await Receive(info, data);
        }
        private async Task Receive(ProxyInfo info)
        {
            info.ProxyPlugin.HandleRequestData(info);
            await Semaphore.WaitAsync();
            bool res = await proxyMessengerSender.Request(info);
            if (res == false)
            {
                clientsManager.TryRemove(info.RequestId, out _);
            }
            Semaphore.Release();
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            ProxyUserToken token = e.UserToken as ProxyUserToken;
            CloseClientSocket(token);
        }
        private void CloseClientSocket(ProxyUserToken token)
        {
            clientsManager.TryRemove(token.Request.RequestId, out _);
            _ = Receive(token, Helper.EmptyArray);
        }
        public void Stop(ushort sourcePort)
        {
            if (serversManager.TryRemove(sourcePort, out ServerInfo model))
            {
                clientsManager.Clear(model.SourcePort);
            }
        }
        public void Stop()
        {
            serversManager.Clear();
            clientsManager.Clear();
        }

        public async Task InputData(ProxyInfo info)
        {
            if (clientsManager.TryGetValue(info.RequestId, out ProxyUserToken token))
            {
                try
                {
                    if (info.Data.Length == 0)
                    {
                        clientsManager.TryRemove(info.RequestId, out _);
                        return;
                    }

                    token.Request.ProxyPlugin.HandleAnswerData(info);
                    if (token.Request.Step == EnumProxyStep.Command)
                    {
                        await token.SourceSocket.SendAsync(info.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                        token.Request.Step = EnumProxyStep.ForwardTcp;
                    }
                    else
                    {
                        await token.SourceSocket.SendAsync(info.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                }
                catch (Exception)
                {
                    clientsManager.TryRemove(info.RequestId, out _);
                }
            }
            else if (info.SourceEP != null)
            {
                try
                {
                    if(ProxyPluginLoader.GetPlugin(info.PluginId,out IProxyPlugin plugin))
                    {
                        plugin.HandleAnswerData(info);
                    }
                    await token.UdpClient.SendAsync(info.Data, info.SourceEP);
                }
                catch (Exception)
                {
                }
            }
        }

    }

    sealed class ProxyUserToken
    {
        public ushort SourcePort { get; set; }
        public Socket SourceSocket { get; set; }
        public UdpClient UdpClient { get; set; }
        public SocketAsyncEventArgs Saea { get; set; }
        public IProxyPlugin ProxyPlugin { get; set; }
        public byte[] PoolBuffer { get; set; }

        public ProxyInfo Request { get; set; } = new ProxyInfo { };
    }

    sealed class ClientsManager
    {
        private ConcurrentDictionary<ulong, ProxyUserToken> clients = new();

        public bool TryAdd(ProxyUserToken model)
        {
            return clients.TryAdd(model.Request.RequestId, model);
        }
        public bool TryGetValue(ulong id, out ProxyUserToken c)
        {
            return clients.TryGetValue(id, out c);
        }
        public bool TryRemove(ulong id, out ProxyUserToken c)
        {
            bool res = clients.TryRemove(id, out c);
            if (res)
            {
                try
                {
                    c.SourceSocket.SafeClose();

                    c.PoolBuffer = Helper.EmptyArray;

                    c.Saea.Dispose();
                    GC.Collect();
                    //  GC.SuppressFinalize(c);
                }
                catch (Exception)
                {
                }
            }
            return res;
        }
        public void Clear(int sourcePort)
        {
            IEnumerable<ulong> requestIds = clients.Where(c => c.Value.SourcePort == sourcePort).Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                TryRemove(requestId, out _);
            }
        }
        public void Clear()
        {
            IEnumerable<ulong> requestIds = clients.Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                TryRemove(requestId, out _);
            }
        }
    }

    sealed class ServersManager
    {
        public ConcurrentDictionary<int, ServerInfo> services = new();
        public bool TryAdd(ServerInfo model)
        {
            return services.TryAdd(model.SourcePort, model);
        }
        public bool Contains(int port)
        {
            return services.ContainsKey(port);
        }
        public bool TryGetValue(int port, out ServerInfo c)
        {
            return services.TryGetValue(port, out c);
        }
        public bool TryRemove(int port, out ServerInfo c)
        {
            bool res = services.TryRemove(port, out c);
            if (res)
            {
                try
                {
                    c.Socket.SafeClose();
                    GC.Collect();
                    //  GC.SuppressFinalize(c);
                }
                catch (Exception)
                {
                }
            }
            return res;
        }
        public void Clear()
        {
            foreach (var item in services.Values)
            {
                try
                {
                    item.Socket.SafeClose();
                    GC.Collect();
                    // GC.SuppressFinalize(item);
                }
                catch (Exception)
                {
                }
            }
            services.Clear();
        }

    }
    sealed class ServerInfo
    {
        public ushort SourcePort { get; set; }
        public Socket Socket { get; set; }
        public UdpClient UdpClient { get; set; }
        public ProxyInfo UdpInfo { get; set; }
    }
}