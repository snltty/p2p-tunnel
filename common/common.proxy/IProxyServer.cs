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
        public bool Start(ushort port, byte plugin, byte rsv = 0);
        public void Stop(byte plugin);
        public void Stop(ushort port);
        public void Stop();
        public void LastError(ushort port, out EnumProxyCommandStatusMsg commandStatusMsg);
        public Task InputData(ProxyInfo info);
    }


    public sealed class ProxyServer : IProxyServer
    {
        private readonly ServersManager serversManager = new ServersManager();
        private readonly ClientsManager clientsManager = new ClientsManager();
        private readonly Config config;
        private NumberSpaceUInt32 requestIdNs = new NumberSpaceUInt32(65536);
        SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        private readonly IProxyMessengerSender proxyMessengerSender;

        public ProxyServer(IProxyMessengerSender proxyMessengerSender, Config config)
        {
            this.proxyMessengerSender = proxyMessengerSender;
            this.config = config;
        }

        public bool Start(ushort port, byte pluginId, byte rsv = 0)
        {
            if (serversManager.Contains(port))
            {
                Logger.Instance.Error($"port:{port} already exists");
                return false;
            }
            if (ProxyPluginLoader.GetPlugin(pluginId, out IProxyPlugin plugin) == false)
            {
                Logger.Instance.Error($"plugin:{pluginId} not exists");
                return false;
            }

            BindAccept(port, plugin, rsv);
            return true;
        }

        private void BindAccept(ushort port, IProxyPlugin plugin, byte rsv)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endpoint);
            socket.Listen(int.MaxValue);

            ServerInfo server = new ServerInfo
            {
                Port = port,
                Socket = socket,
                UdpClient = new UdpClient(endpoint),
                ProxyPlugin = plugin
            };
            server.UdpClient.EnableBroadcast = true;
            server.UdpClient.Client.WindowsUdpBug();
            serversManager.TryAdd(server);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            ProxyUserToken token = new ProxyUserToken
            {
                Server = server,
                Rsv = rsv,
                Saea = acceptEventArg,
                Request = new ProxyInfo
                {
                    ProxyPlugin = plugin,
                    Step = EnumProxyStep.ForwardUdp,
                    Command = EnumProxyCommand.UdpAssociate,
                    CommandStatus = EnumProxyCommandStatus.ConnecSuccess,
                    PluginId = plugin.Id,
                    ListenPort = port,
                    Rsv = rsv,
                    BufferSize = plugin.BufferSize,
                    RequestId = port
                }
            };
            clientsManager.TryAdd(token);
            acceptEventArg.UserToken = token;
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            plugin.Started(port);

            IAsyncResult result = server.UdpClient.BeginReceive(ProcessReceiveUdp, token);
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            try
            {

                acceptEventArg.AcceptSocket = null;
                ProxyUserToken token = ((ProxyUserToken)acceptEventArg.UserToken);
                if (token.Server.Socket.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
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
                    Socket = e.AcceptSocket,
                    Server = acceptToken.Server,
                    Request = new ProxyInfo
                    {
                        RequestId = id,
                        BufferSize = acceptToken.Server.ProxyPlugin.BufferSize,
                        Command = EnumProxyCommand.Connect,
                        AddressType = EnumProxyAddressType.IPV4,
                        ListenPort = acceptToken.Server.Port,
                        Step = EnumProxyStep.Command,
                        PluginId = acceptToken.Server.ProxyPlugin.Id,
                        ProxyPlugin = acceptToken.Server.ProxyPlugin,
                        Rsv = acceptToken.Rsv,
                        ClientEP = e.AcceptSocket.RemoteEndPoint as IPEndPoint
                    },
                };
                clientsManager.TryAdd(token);

                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None
                };
                token.Saea = readEventArgs;

                token.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
                token.Socket.SendTimeout = 5000;
                token.PoolBuffer = new byte[(1 << (byte)acceptToken.Server.ProxyPlugin.BufferSize) * 1024];
                readEventArgs.SetBuffer(token.PoolBuffer, 0, (1 << (byte)acceptToken.Server.ProxyPlugin.BufferSize) * 1024);
                readEventArgs.Completed += IO_Completed;

                if (token.Socket.ReceiveAsync(readEventArgs) == false)
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
            ProxyUserToken token = (ProxyUserToken)e.UserToken;
            try
            {

                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int totalLength = e.BytesTransferred;
                    token.Request.Data = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    //有些客户端，会把一个包拆开发送，很奇怪，不得不验证一下数据完整性
                    if (token.Request.Step == EnumProxyStep.Command)
                    {
                        EnumProxyValidateDataResult validate = token.Request.ProxyPlugin.ValidateData(token.Request);
                        if ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
                        {
                            //太短
                            while ((validate & EnumProxyValidateDataResult.TooShort) == EnumProxyValidateDataResult.TooShort)
                            {
                                totalLength += await token.Socket.ReceiveAsync(e.Buffer.AsMemory(e.Offset + totalLength), SocketFlags.None);
                                token.Request.Data = e.Buffer.AsMemory(e.Offset, totalLength);
                                validate = token.Request.ProxyPlugin.ValidateData(token.Request);
                            }
                        }

                        //不短，又不相等，直接关闭连接
                        if ((validate & EnumProxyValidateDataResult.Equal) != EnumProxyValidateDataResult.Equal)
                        {
                            CloseClientSocket(e);
                            return;
                        }

                    }
                    bool receive = token.Receive;
                    await Receive(token.Request);
                    if (receive)
                    {
                        if (token.Socket.Available > 0 && token.Request.Step > EnumProxyStep.Command)
                        {
                            while (token.Socket.Available > 0)
                            {
                                int length = await token.Socket.ReceiveAsync(e.Buffer.AsMemory(), SocketFlags.None);
                                if (length > 0)
                                {
                                    token.Request.Data = e.Buffer.AsMemory(0, length);
                                    await Receive(token.Request);
                                }
                            }
                        }
                        if (token.Socket.Connected)
                        {
                            if (token.Socket.ReceiveAsync(e) == false)
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
                        if (token.Socket.Connected == false)
                        {
                            CloseClientSocket(e);
                        }
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
                Logger.Instance.Error($"{token.Request.RequestId} {ex + ""}");
            }
        }

        private async void ProcessReceiveUdp(IAsyncResult result)
        {
            IPEndPoint rep = null;
            try
            {
                ProxyUserToken token = result.AsyncState as ProxyUserToken;

                token.Request.Data = token.Server.UdpClient.EndReceive(result, ref rep);
                token.Request.SourceEP = rep;
                token.Request.ClientEP = rep;
                await Receive(token.Request);
                token.Request.Data = Helper.EmptyArray;

                result = token.Server.UdpClient.BeginReceive(ProcessReceiveUdp, token);
            }
            catch (Exception)
            {
                //Logger.Instance.Error($"socks5 listen udp -> error " + ex);
            }
        }

        private async Task Receive(ProxyUserToken token, Memory<byte> data)
        {
            await Receive(token.Request, data);
        }
        private async Task Receive(ProxyInfo info, Memory<byte> data)
        {
            info.Data = data;
            await Receive(info);
        }
        private async Task Receive(ProxyInfo info)
        {
            Logger.Instance.DebugDebug($"proxy receive:{info.RequestId}");
            await Semaphore.WaitAsync();
            Logger.Instance.DebugDebug($"proxy receive1:{info.RequestId}");
            try
            {
                Logger.Instance.DebugDebug($"proxy receive2:{info.RequestId}");
                if (info.Data.Length > 0 || info.Step > EnumProxyStep.Command)
                {
                    Logger.Instance.DebugDebug($"proxy receive3:{info.RequestId}");
                    if (info.ProxyPlugin.HandleRequestData(info))
                    {
                        Logger.Instance.DebugDebug($"proxy receive4:{info.RequestId}");
                        BuildHeaders(info);
                        bool res = await proxyMessengerSender.Request(info);
                        Logger.Instance.DebugDebug($"proxy receive5:{info.RequestId}");
                        if (res == false)
                        {
                            Logger.Instance.DebugDebug($"proxy receive6:{info.RequestId}");
                            if (info.Step == EnumProxyStep.Command)
                            {
                                Logger.Instance.DebugDebug($"proxy receive7:{info.RequestId}");
                                info.CommandStatus = EnumProxyCommandStatus.NetworkError;
                                info.CommandStatusMsg = EnumProxyCommandStatusMsg.Connection;
                                await InputData(info);
                                Logger.Instance.DebugDebug($"proxy receive8:{info.RequestId}");
                            }
                            else if (info.Step == EnumProxyStep.ForwardTcp)
                            {
                                clientsManager.TryRemove(info.RequestId, out _);
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            Logger.Instance.DebugDebug($"proxy receive9:{info.RequestId}");
            Semaphore.Release();
        }
        private void BuildHeaders(ProxyInfo info)
        {
            info.Headers = Helper.EmptyArray;
            if (info.ProxyPlugin.Headers != null)
            {
                //缓存为空 或者 不是最新数据
                if (info.ProxyPlugin.HeadersBytes.Length == 0 || ReferenceEquals(info.ProxyPlugin.Headers, info.HeadersCache) == false)
                {
                    info.HeadersCache = info.ProxyPlugin.Headers;
                    info.ProxyPlugin.HeadersBytes = info.HeadersCache.Build();
                }
                info.HttpIndex = HttpParser.IsHttp(info.Data);
                if (info.HttpIndex > 0)
                {
                    info.HttpIndex += 2;
                    info.Headers = info.ProxyPlugin.HeadersBytes;
                }
            }
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

        public void Stop(byte plugin)
        {
            List<ushort> ports = serversManager.services.Where(c => c.Value.ProxyPlugin.Id == plugin).Select(c => c.Key).ToList();
            foreach (var item in ports)
            {
                if (serversManager.TryRemove(item, out ServerInfo model))
                {
                    clientsManager.Clear(model.Port);
                }
            }
        }
        public void Stop(ushort port)
        {
            if (serversManager.TryRemove(port, out ServerInfo model))
            {
                clientsManager.Clear(model.Port);
                clientsManager.TryRemove((ulong)model.Port, out _);
            }
        }
        public void Stop()
        {
            serversManager.Clear();
            clientsManager.Clear();
        }

        public void LastError(ushort port, out EnumProxyCommandStatusMsg commandStatusMsg)
        {
            commandStatusMsg = EnumProxyCommandStatusMsg.Success;
            if (serversManager.TryGetValue(port, out ServerInfo model))
            {
                commandStatusMsg = model.LastError;
            }
        }

        public async Task InputData(ProxyInfo info)
        {
            try
            {
                if (clientsManager.TryGetValue(info.RequestId, out ProxyUserToken token) == false)
                {
                    return;
                }
                token.Request.CommandStatusMsg = info.CommandStatusMsg;
                token.Server.LastError = info.CommandStatusMsg;
                EnumProxyStep step = info.Step;

                if (info.Data.Length == 0)
                {
                    clientsManager.TryRemove(info.RequestId, out _);
                    return;
                }

                if (step == EnumProxyStep.Command)
                {
                    //command步骤的魔术数据，直接返回一个数据就好了，用于测试
                    if (ProxyHelper.GetIsMagicData(info.Data))
                    {
                        info.Data.Span[0] = (byte)info.CommandStatusMsg;
                        await token.Socket.SendAsync(info.Data, SocketFlags.None);
                        return;
                    }

                    if (token.Receive == false)
                    {
                        token.Receive = true;
                        if (token.Socket.ReceiveAsync(token.Saea) == false)
                        {
                            ProcessReceive(token.Saea);
                        }
                    }
                }

                EnumProxyCommandStatus status = info.CommandStatus;
                //数据后处理，组织回复数据，及是否回复
                bool res = token.Request.ProxyPlugin.HandleAnswerData(info);
                token.Request.Step = info.Step;
                token.Request.Command = info.Command;
                token.Request.Rsv = info.Rsv;
                //需要回复数据
                if (res)
                {
                    if (info.Step == EnumProxyStep.ForwardUdp)
                    {
                        await token.Server.UdpClient.SendAsync(info.Data, info.SourceEP);
                    }
                    else
                    {
                        await token.Socket.SendAsync(info.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                }
                //是失败的
                if (step == EnumProxyStep.Command && status != EnumProxyCommandStatus.ConnecSuccess)
                {
                    clientsManager.TryRemove(info.RequestId, out _);
                    return;
                }

            }
            catch (Exception ex)
            {
                clientsManager.TryRemove(info.RequestId, out _);
                Logger.Instance.DebugError(ex);
            }
        }
    }

    sealed class ProxyUserToken
    {
        public byte Rsv { get; set; }
        public Socket Socket { get; set; }
        public ServerInfo Server { get; set; }
        public SocketAsyncEventArgs Saea { get; set; }
        public byte[] PoolBuffer { get; set; }
        public bool Receive { get; set; }

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
                    c?.Socket.SafeClose();
                    c.PoolBuffer = Helper.EmptyArray;
                    c?.Saea.Dispose();
                    GC.Collect();
                    //  GC.SuppressFinalize(c);
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            }
            return res;
        }
        public void Clear(int sourcePort)
        {
            IEnumerable<ulong> requestIds = clients.Where(c => c.Value.Server.Port == sourcePort).Select(c => c.Key);
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
        public ConcurrentDictionary<ushort, ServerInfo> services = new();
        public bool TryAdd(ServerInfo model)
        {
            return services.TryAdd(model.Port, model);
        }

        public bool Contains(ushort port)
        {
            return services.ContainsKey(port);
        }
        public bool TryGetValue(ushort port, out ServerInfo c)
        {
            return services.TryGetValue(port, out c);
        }

        public bool TryRemove(ushort port, out ServerInfo c)
        {
            bool res = services.TryRemove(port, out c);
            if (res)
            {
                try
                {
                    c.ProxyPlugin.Stoped(port);
                    c.Socket.SafeClose();
                    c.UdpClient.Dispose();
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
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
                    item.ProxyPlugin.Stoped(item.Port);
                    item.Socket.SafeClose();
                    GC.Collect();
                    // GC.SuppressFinalize(item);
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            }
            services.Clear();
        }

    }
    sealed class ServerInfo
    {
        public IProxyPlugin ProxyPlugin { get; set; }
        public ushort Port { get; set; }
        public Socket Socket { get; set; }
        public UdpClient UdpClient { get; set; }
        public EnumProxyCommandStatusMsg LastError { get; set; }

    }
}