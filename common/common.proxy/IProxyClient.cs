using common.libs;
using common.libs.extends;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private readonly WheelTimer<object> wheelTimer;

        public ProxyClient(WheelTimer<object> wheelTimer, IProxyMessengerSender proxyMessengerSender, Config config)
        {
            this.wheelTimer = wheelTimer;
            this.proxyMessengerSender = proxyMessengerSender;
            this.config = config;
            TimeoutUdp();
        }

        public async Task InputData(ProxyInfo info)
        {
            bool pluginExists = ProxyPluginLoader.GetPlugin(info.PluginId, out IProxyPlugin plugin);
            if (info.Step == EnumProxyStep.Command)
            {
                if (pluginExists == false || plugin.ValidateAccess(info) == false)
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.CommandNotAllow);
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
                await ForwardUdp(info, plugin);
            }
        }

        private async Task Command(ProxyInfo info)
        {
            try
            {
                if (info.Command == EnumProxyCommand.Connect)
                {
                    if (FirewallDenied(info, FirewallProtocolType.TCP))
                    {
                        _ = ConnectReponse(info, EnumProxyCommandStatus.AddressNotAllow);
                    }
                    else
                    {
                        Connect(info);
                    }
                }
                else if (info.Command == EnumProxyCommand.UdpAssociate)
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.ConnecSuccess);
                }
                else if (info.Command == EnumProxyCommand.Bind)
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.CommandNotAllow);
                }
                else
                {
                    _ = ConnectReponse(info, EnumProxyCommandStatus.CommandNotAllow);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                _ = ConnectReponse(info, EnumProxyCommandStatus.ConnectFail);
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
                if (info.Data.Length > 0 && token.TargetSocket.Connected)
                {
                    try
                    {
                        await token.TargetSocket.SendAsync(info.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception)
                    {
                        CloseClientSocket(token);
                    }
                }
                else
                {
                    CloseClientSocket(token);
                }
            }
        }
        private async Task ForwardUdp(ProxyInfo info, IProxyPlugin plugin)
        {
            IPEndPoint remoteEndpoint = ReadRemoteEndPoint(info);
            if (remoteEndpoint.Port == 0) return;

            bool isBroadcast = info.TargetAddress.GetIsBroadcastAddress();
            if (isBroadcast && plugin.BroadcastBind.Equals(IPAddress.Any))
            {
                remoteEndpoint.Address = IPAddress.Loopback;
            }

            ConnectionKeyUdp key = new ConnectionKeyUdp(info.Connection.ConnectId, info.SourceEP);
            try
            {

                if (udpConnections.TryGetValue(key, out UdpToken token) == false)
                {
                    if (FirewallDenied(info, FirewallProtocolType.UDP))
                    {
                        return;
                    }

                    Socket socket = new Socket(remoteEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    if (isBroadcast)
                    {
                        socket.Bind(new IPEndPoint(plugin.BroadcastBind, 0));
                        socket.EnableBroadcast = true;
                    }
                    socket.WindowsUdpBug();
                    token = new UdpToken { Data = info, TargetSocket = socket, Key = key };
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
                    token.Update();
                    await token.TargetSocket.SendToAsync(info.Data, SocketFlags.None, remoteEndpoint);
                    token.Data.Data = Helper.EmptyArray;
                }
            }
            catch (Exception ex)
            {
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
            }, 1000, true);
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
                if (udpConnections.TryRemove(token.Key, out _))
                {
                    token.Clear();
                }
                //Logger.Instance.DebugError($"socks5 forward udp -> receive" + ex);
            }
        }

        private void Connect(ProxyInfo info)
        {
            IPEndPoint remoteEndpoint = ReadRemoteEndPoint(info);
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
                    int length = token.Data.Data.Length;
                    if (length > 0)
                    {
                        await token.TargetSocket.SendAsync(token.Data.Data, SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                        token.Data.Data = Helper.EmptyArray;
                    }
                    await ConnectReponse(token.Data, EnumProxyCommandStatus.ConnecSuccess);
                    token.Data.TargetAddress = Helper.EmptyArray;
                    token.Data.Step = EnumProxyStep.ForwardTcp;

                    BindTargetReceive(token);
                    return;
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
                    await ConnectReponse(token.Data, command);
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                command = EnumProxyCommandStatus.ServerError;
                await ConnectReponse(token.Data, command);
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
            try
            {
                AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    if (token.Data.Step == EnumProxyStep.Command)
                    {
                        await ConnectReponse(token.Data, EnumProxyCommandStatus.ConnecSuccess);
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
                Logger.Instance.DebugError(ex);
            }
        }

        private async Task ConnectReponse(ProxyInfo info, EnumProxyCommandStatus command)
        {
            info.Response[0] = (byte)command;
            info.Data = info.Response;
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
            await Semaphore.WaitAsync();
            bool res = await proxyMessengerSender.Response(info);
            Semaphore.Release();
            return res;
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
                connections.TryRemove(token.Key, out _);
                // _ = socks5MessengerSender.ResponseClose(token.Data);
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

        /// <summary>
        /// 防火墙阻止
        /// </summary>
        /// <param name="info"></param>
        /// <param name="protocolType"></param>
        /// <returns></returns>
        private bool FirewallDenied(ProxyInfo info, FirewallProtocolType protocolType)
        {
            //阻止IPV6的内网ip
            if (info.TargetAddress.Length == EndPointExtends.ipv6Loopback.Length)
            {
                Span<byte> span = info.TargetAddress.Span;
                return span.SequenceEqual(EndPointExtends.ipv6Loopback.Span)
                     || span.SequenceEqual(EndPointExtends.ipv6Multicast.Span)
                     || (span[0] == EndPointExtends.ipv6Local.Span[0] && span[1] == EndPointExtends.ipv6Local.Span[1]);
            }
            //IPV4的，防火墙验证
            else if (info.TargetAddress.Length == 4)
            {
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(info.TargetAddress.Span);
                FirewallKey key = new FirewallKey(info.TargetPort, protocolType);
                FirewallKey key0 = new FirewallKey(0, protocolType);
                //局域网或者组播，验证白名单
                if (info.TargetAddress.IsLan() || info.TargetAddress.GetIsBroadcastAddress())
                {
                    if (config.AllowFirewalls.Count > 0)
                    {
                        if (config.AllowFirewalls.TryGetValue(key, out FirewallCache cache) || config.AllowFirewalls.TryGetValue(key0, out cache))
                        {
                            for (int i = 0; i < cache.IPs.Length; i++)
                            {
                                //有一项通过就通过
                                if ((ip & cache.IPs[i].MaskValue) == cache.IPs[i].NetWork)
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                }
                //黑名单
                if (config.DeniedFirewalls.Count > 0)
                {
                    if (config.DeniedFirewalls.TryGetValue(key, out FirewallCache cache) || config.DeniedFirewalls.TryGetValue(key0, out cache))
                    {
                        for (int i = 0; i < cache.IPs.Length; i++)
                        {
                            //有一项匹配就不通过
                            if ((ip & cache.IPs[i].MaskValue) == cache.IPs[i].NetWork)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            //其它的直接通过
            return false;
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
