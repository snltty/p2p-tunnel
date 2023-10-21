using common.libs;
using common.libs.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace common.proxy
{
    public interface IProxyClient
    {
        Task InputData(ProxyInfo data);
    }

    public sealed class ProxyClient : IProxyClient
    {
        private ConcurrentDictionary<ConnectionKey, AsyncServerUserToken> connections = new(new ConnectionKeyComparer());
        private ConcurrentDictionary<ConnectionKeyUdp, UdpToken> udpConnections = new(new ConnectionKeyUdpComparer());
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        private readonly IProxyMessengerSender proxyMessengerSender;
        private readonly Config config;
        private readonly ProxyPluginValidatorHandler pluginValidatorHandler;

        private readonly WheelTimer<object> wheelTimer;

        public ProxyClient(WheelTimer<object> wheelTimer, IProxyMessengerSender proxyMessengerSender, Config config, ProxyPluginValidatorHandler pluginValidatorHandler)
        {
            this.wheelTimer = wheelTimer;
            this.proxyMessengerSender = proxyMessengerSender;
            this.config = config;
            this.pluginValidatorHandler = pluginValidatorHandler;
            TimeoutUdp();
        }

        public async Task InputData(ProxyInfo info)
        {
            if (info.Step == EnumProxyStep.Command)
            {
                info.IsMagicData = ProxyHelper.GetIsMagicData(info.Data);
                if (ProxyPluginLoader.GetPlugin(info.PluginId, out IProxyPlugin plugin) == false)
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.ServerError, EnumProxyCommandStatusMsg.Plugin);
                    return;
                }
                info.ProxyPlugin = plugin;
                if (pluginValidatorHandler.Validate(info) == false)
                {
                    EnumProxyCommandStatusMsg statusMsg = info.CommandStatusMsg;
                    _ = ConnectReponse(info, EnumProxyCommandStatus.CommandNotAllow, statusMsg);
                    return;
                }
                _ = Command(info);
            }
            else if (info.Step == EnumProxyStep.ForwardTcp)
            {
                await ForwardTcp(info);
            }
            else if (info.Step == EnumProxyStep.ForwardUdp)
            {
                await ForwardUdp(info);
            }
        }
        private async Task Command(ProxyInfo info)
        {
            try
            {
                if (info.Command == EnumProxyCommand.Connect)
                {
                    Connect(info);
                }
                else if (info.Command == EnumProxyCommand.UdpAssociate)
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.ConnecSuccess, EnumProxyCommandStatusMsg.Success);
                }
                else if (info.Command == EnumProxyCommand.Bind)
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.CommandNotAllow, EnumProxyCommandStatusMsg.Connect);
                }
                else
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.CommandNotAllow, EnumProxyCommandStatusMsg.Connect);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                _ = ConnectReponse(info, EnumProxyCommandStatus.ConnectFail, EnumProxyCommandStatusMsg.Connect);
                return;
            }
            await Task.CompletedTask;

        }
        private async Task ForwardTcp(ProxyInfo info)
        {
            ConnectionKey key = new ConnectionKey(info.Connection.ConnectId, info.RequestId);
            if (connections.TryGetValue(key, out AsyncServerUserToken token))
            {
                token.Data.Step = info.Step;
                token.Data.Command = info.Command;
                token.Data.Rsv = info.Rsv;
                token.Data.Connection = info.Connection;
                if (info.Data.Length > 0 && token.TargetSocket.Connected)
                {
                    try
                    {
                        //Console.WriteLine($"forward-rid:{token.Data.RequestId}-{token.TargetSocket.LocalEndPoint}->{token.TargetSocket.RemoteEndPoint}-{info.Data.Length}");
                        int length = await token.TargetSocket.SendAsync(info.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                        //Console.WriteLine($"forward-rid2:{token.Data.RequestId}-{token.TargetSocket.LocalEndPoint}->{token.TargetSocket.RemoteEndPoint}-{length}");
                    }
                    catch (Exception ex)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            Logger.Instance.Error($"proxy forward tcp send :{ex}");
                        }
                        CloseClientSocket(token);
                    }
                }
                else
                {
                    CloseClientSocket(token);
                }
            }
        }
        private async Task ForwardUdp(ProxyInfo info)
        {
            ConnectionKeyUdp key = new ConnectionKeyUdp(info.Connection.ConnectId, info.SourceEP);
            try
            {
                if (udpConnections.TryGetValue(key, out UdpToken token) == false)
                {
                    IPEndPoint remoteEndpoint = ReadRemoteEndPoint(info);
                    if (remoteEndpoint.Port == 0) return;

                    if (ProxyPluginLoader.GetPlugin(info.PluginId, out IProxyPlugin plugin) == false) return;
                    info.ProxyPlugin = plugin;
                    if (pluginValidatorHandler.Validate(info) == false)
                    {
                        return;
                    }
                    bool isBroadcast = info.TargetAddress.GetIsBroadcastAddress();
                    if (isBroadcast && plugin.BroadcastBind.Equals(IPAddress.Any))
                    {
                        remoteEndpoint.Address = IPAddress.Loopback;
                    }

                    Socket socket = new Socket(remoteEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    if (isBroadcast)
                    {
                        socket.Bind(new IPEndPoint(plugin.BroadcastBind, 0));
                        socket.EnableBroadcast = true;
                    }
                    socket.WindowsUdpBug();
                    token = new UdpToken { Data = info, TargetSocket = socket, Key = key, TargetEP = remoteEndpoint };
                    token.PoolBuffer = new byte[65535];
                    udpConnections.AddOrUpdate(key, token, (a, b) => token);

                    await token.TargetSocket.SendToAsync(info.Data, SocketFlags.None, remoteEndpoint);
                    token.Data.Data = Helper.EmptyArray;
                    if (isBroadcast == false)
                    {
                        IAsyncResult result = socket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
                    }
                }
                else
                {
                    token.Data.Step = info.Step;
                    token.Data.Command = info.Command;
                    token.Data.Rsv = info.Rsv;
                    token.Data.Connection = info.Connection;
                    token.Update();
                    await token.TargetSocket.SendToAsync(info.Data, SocketFlags.None, token.TargetEP);
                    token.Data.Data = Helper.EmptyArray;
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error($"proxy forward udp send :{ex}");
                }
                if (udpConnections.TryRemove(key, out UdpToken _token))
                {
                    _token.Clear();
                }
                //Logger.Instance.DebugError($"socks5 forward udp -> sendto {remoteEndpoint} : {info.Data.Length}  " + ex);
            }
        }
        private void TimeoutUdp()
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                State = null,
                Callback = (timeout) =>
                {
                    long time = DateTimeHelper.GetTimeStamp();

                    var tokens = udpConnections.Where(c => time - c.Value.LastTime > (60 * 1000));
                    foreach (var item in tokens)
                    {
                        if (udpConnections.TryRemove(item.Key, out _))
                        {
                            item.Value.Clear();
                        }
                    }
                }
            }, 5000, true);
        }
        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            UdpToken token = result.AsyncState as UdpToken;
            try
            {
                int length = token.TargetSocket.EndReceiveFrom(result, ref token.TempRemoteEP);

                if (length > 0)
                {
                    token.Data.Data = token.PoolBuffer.AsMemory(0, length);

                    token.Update();
                    await Receive(token.Data);
                    token.Data.Data = Helper.EmptyArray;
                }
                result = token.TargetSocket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error($"socks5 forward udp -> receive" + ex);
                }
                if (udpConnections.TryRemove(token.Key, out _))
                {
                    token.Clear();
                }
            }
        }

        private void Connect(ProxyInfo info)
        {
            IPEndPoint remoteEndpoint = ReadRemoteEndPoint(info);
            //Console.WriteLine($"connect {remoteEndpoint}");
            Socket socket = new Socket(remoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
            socket.SendTimeout = 5000;

            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = socket,
                Data = info,
                Key = new ConnectionKey(info.Connection.ConnectId, info.RequestId)
            };
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None
            };
            connectEventArgs.RemoteEndPoint = remoteEndpoint;
            connectEventArgs.Completed += Target_IO_Completed;
            if (socket.ConnectAsync(connectEventArgs) == false)
            {
                TargetProcessConnect(connectEventArgs);
            }
        }
        private void Target_IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    TargetProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    TargetProcessReceive(e);
                    break;
                default:
                    break;
            }
        }
        private async void TargetProcessConnect(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
            EnumProxyCommandStatus command = EnumProxyCommandStatus.ServerError;
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    //Console.WriteLine($"connect-rid:{token.Data.RequestId}-{token.TargetSocket.LocalEndPoint}->{token.TargetSocket.RemoteEndPoint}");
                    if (token.Data.Data.Length > 0 && token.Data.IsMagicData == false)
                    {
                        //Console.WriteLine($"forward-rid:{token.Data.RequestId}-{token.TargetSocket.LocalEndPoint}->{token.TargetSocket.RemoteEndPoint}-{Encoding.UTF8.GetString(token.Data.Data.Span)}");
                        await token.TargetSocket.SendAsync(token.Data.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    await ConnectReponse(token.Data, EnumProxyCommandStatus.ConnecSuccess, EnumProxyCommandStatusMsg.Success);
                    token.Data.Step = EnumProxyStep.ForwardTcp;
                    token.Data.Data = Helper.EmptyArray;
                    if (token.Data.IsMagicData)
                    {
                        CloseClientSocket(token);
                        return;
                    }
                    token.Data.TargetAddress = Helper.EmptyArray;
                    BindTargetReceive(token);
                }
                else
                {
                    if (e.SocketError == SocketError.ConnectionRefused)
                    {
                        command = EnumProxyCommandStatus.DistReject;
                    }
                    else if (e.SocketError == SocketError.NetworkDown)
                    {
                        command = EnumProxyCommandStatus.NetworkError;
                    }
                    else if (e.SocketError == SocketError.ConnectionReset)
                    {
                        command = EnumProxyCommandStatus.DistReject;
                    }
                    else if (e.SocketError == SocketError.AddressFamilyNotSupported || e.SocketError == SocketError.OperationNotSupported)
                    {
                        command = EnumProxyCommandStatus.AddressNotAllow;
                    }
                    else
                    {
                        command = EnumProxyCommandStatus.ServerError;
                    }
                    await ConnectReponse(token.Data, command, EnumProxyCommandStatusMsg.Connect);
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
                command = EnumProxyCommandStatus.ServerError;
                await ConnectReponse(token.Data, command, EnumProxyCommandStatusMsg.Connect);
                CloseClientSocket(token);
            }
        }

        private void BindTargetReceive(AsyncServerUserToken connectToken)
        {
            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = connectToken.TargetSocket,
                Key = connectToken.Key,
                Data = connectToken.Data
            };
            connections.TryAdd(token.Key, token);
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            token.PoolBuffer = new byte[(1 << (byte)token.Data.BufferSize) * 1024];
            readEventArgs.SetBuffer(token.PoolBuffer, 0, (1 << (byte)token.Data.BufferSize) * 1024);
            readEventArgs.Completed += Target_IO_Completed;

            if (token.TargetSocket.ReceiveAsync(readEventArgs) == false)
            {
                TargetProcessReceive(readEventArgs);
            }
        }
        private async void TargetProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //Console.WriteLine($"response-rid:{token.Data.RequestId}-{Encoding.UTF8.GetString(e.Buffer.AsMemory(e.Offset, e.BytesTransferred).Span)}");
                    if (token.Data.Step == EnumProxyStep.Command)
                    {
                        await ConnectReponse(token.Data, EnumProxyCommandStatus.ConnecSuccess, EnumProxyCommandStatusMsg.Success);
                        token.Data.Step = EnumProxyStep.ForwardTcp;
                    }

                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    token.Data.Data = e.Buffer.AsMemory(offset, length);
                    await Receive(token);
                    token.Data.Data = Helper.EmptyArray;

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = await token.TargetSocket.ReceiveAsync(e.Buffer.AsMemory(), SocketFlags.None);
                            if (length > 0)
                            {
                                token.Data.Data = e.Buffer.AsMemory(0, length);
                                await Receive(token);
                                token.Data.Data = Helper.EmptyArray;
                            }
                        }
                    }

                    if (token.TargetSocket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        TargetProcessReceive(e);
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
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
        }

        private async Task ConnectReponse(ProxyInfo info, EnumProxyCommandStatus command, EnumProxyCommandStatusMsg commandStatusMsg)
        {
            if (info.IsMagicData == false)
            {
                info.Data = Helper.TrueArray;
            }

            info.CommandStatus = command;
            info.CommandStatusMsg = commandStatusMsg;
            await Receive(info);
        }
        private async Task<bool> Receive(AsyncServerUserToken token)
        {
            bool res = await Receive(token.Data);
            if (res == false)
            {
                CloseClientSocket(token);
            }
            return res;
        }
        private async Task<bool> Receive(ProxyInfo info)
        {
<<<<<<< HEAD
            //Console.WriteLine($"response1-rid:{info.RequestId}-{Encoding.UTF8.GetString(info.Data.Span)}");
            await Semaphore.WaitAsync();
            bool res = await proxyMessengerSender.Response(info);
            Semaphore.Release();

            return res;
=======
            await Semaphore.WaitAsync();
            try
            {
                return await proxyMessengerSender.Response(info);
            }
            catch (Exception)
            {

            }
            finally
            {
                Semaphore.Release();
            }
            return false;
>>>>>>> 0776db30923e8ee83814d584a55fe9169a4d1992
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = e.UserToken as AsyncServerUserToken;
            if (CloseClientSocket(token))
            {
                e.Dispose();
            }
        }
        private bool CloseClientSocket(AsyncServerUserToken token)
        {
            if (token.IsClosed == false)
            {
                token.IsClosed = true;
                token.Clear();
                token.Data.Data = Helper.TrueArray;
                _ = Receive(token.Data);
                connections.TryRemove(token.Key, out _);
                return true;
            }
            return false;
        }

        private IPEndPoint ReadRemoteEndPoint(ProxyInfo info)
        {
            IPAddress ip = IPAddress.Any;
            switch (info.AddressType)
            {
                case EnumProxyAddressType.IPV4:
                case EnumProxyAddressType.IPV6:
                    {
                        ip = new IPAddress(info.TargetAddress.Span);
                    }
                    break;
                case EnumProxyAddressType.Domain:
                    {
                        ip = NetworkHelper.GetDomainIp(info.TargetAddress.GetString());
                    }
                    break;
                default:
                    break;
            }
            return new IPEndPoint(ip, info.TargetPort);
        }

    }

    public sealed class AsyncServerUserToken
    {
        public ConnectionKey Key { get; set; }
        public Socket TargetSocket { get; set; }
        public ProxyInfo Data { get; set; }
        public bool IsClosed { get; set; } = false;
        public byte[] PoolBuffer { get; set; }
        public void Clear()
        {
            TargetSocket?.SafeClose();

            PoolBuffer = Helper.EmptyArray;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
    public sealed class ConnectionKeyComparer : IEqualityComparer<ConnectionKey>
    {
        public bool Equals(ConnectionKey x, ConnectionKey y)
        {
            return x.RequestId == y.RequestId && x.ConnectId == y.ConnectId;
        }
        public int GetHashCode(ConnectionKey obj)
        {
            return obj.RequestId.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    public readonly struct ConnectionKey
    {
        public readonly uint RequestId { get; }
        public readonly ulong ConnectId { get; }
        public ConnectionKey(ulong connectId, uint requestId)
        {
            ConnectId = connectId;
            RequestId = requestId;
        }
    }

    public sealed class UdpToken
    {
        public ConnectionKeyUdp Key { get; set; }
        public Socket TargetSocket { get; set; }
        public ProxyInfo Data { get; set; }
        public byte[] PoolBuffer { get; set; }
        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();
        public EndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        public EndPoint TargetEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        public void Clear()
        {
            TargetSocket?.SafeClose();
            PoolBuffer = Helper.EmptyArray;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
        public void Update()
        {
            LastTime = DateTimeHelper.GetTimeStamp();
        }
    }
    public sealed class ConnectionKeyUdpComparer : IEqualityComparer<ConnectionKeyUdp>
    {
        public bool Equals(ConnectionKeyUdp x, ConnectionKeyUdp y)
        {
            return x.Source != null && x.Source.Equals(y.Source) && x.ConnectId == y.ConnectId;
        }
        public int GetHashCode(ConnectionKeyUdp obj)
        {
            if (obj.Source == null) return 0;
            return obj.Source.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    public readonly struct ConnectionKeyUdp
    {
        public readonly IPEndPoint Source { get; }
        public readonly ulong ConnectId { get; }
        public ConnectionKeyUdp(ulong connectId, IPEndPoint source)
        {
            ConnectId = connectId;
            Source = source;
        }
    }
}
