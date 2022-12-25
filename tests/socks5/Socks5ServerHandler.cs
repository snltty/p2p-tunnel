using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace socks5
{
    /// <summary>
    /// 
    /// </summary>
    public class Socks5ServerHandler : ISocks5ServerHandler
    {
        private ConcurrentDictionary<ConnectionKey, AsyncServerUserToken> connections = new(new ConnectionKeyComparer());
        private ConcurrentDictionary<ConnectionKeyUdp, UdpToken> udpConnections = new(new ConnectionKeyUdpComparer());
        private readonly Dictionary<Socks5EnumStep, Action<Socks5Info>> handles = new Dictionary<Socks5EnumStep, Action<Socks5Info>>();

        private readonly WheelTimer<object> wheelTimer;
        public Action<Socks5Info> OnSendResponse { get; set; } = (data) => { };
        public Action<Socks5Info> OnSendClose { get; set; } = (data) => { };

        public Socks5ServerHandler(WheelTimer<object> wheelTimer)
        {
            this.wheelTimer = wheelTimer;
            TimeoutUdp();

            handles = new Dictionary<Socks5EnumStep, Action<Socks5Info>> {
                {Socks5EnumStep.Request, HandleRequest},
                {Socks5EnumStep.Auth, HandleAuth},
                {Socks5EnumStep.Command, HandleCommand},
                {Socks5EnumStep.Forward, HndleForward},
                {Socks5EnumStep.ForwardUdp, HndleForwardUdp},
            };

        }

        public void InputData(Socks5Info data)
        {
            Socks5EnumStep step = data.Socks5Step;
            if (handles.TryGetValue(step, out Action<Socks5Info> action))
            {
                action(data);
            }
        }

        private void HandleRequest(Socks5Info info)
        {
            info.Response[0] = (byte)Socks5EnumAuthType.NoAuth;
            info.Data = info.Response;

            OnSendResponse(info);
        }
        private void HandleAuth(Socks5Info info)
        {
            info.Response[0] = (byte)Socks5EnumAuthState.Success;
            info.Data = info.Response;
            OnSendResponse(info);
        }
        private void HndleForward(Socks5Info info)
        {
            ConnectionKey key = new ConnectionKey(0, info.Id);
            if (connections.TryGetValue(key, out AsyncServerUserToken token))
            {
                if (info.Data.Length > 0 && token.TargetSocket.Connected)
                {
                    try
                    {
                        _ = token.TargetSocket.Send(info.Data.Span, SocketFlags.None);
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
        private void HndleForwardUdp(Socks5Info info)
        {
            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(info.Data, out Span<byte> ipMemory);
            Memory<byte> sendData = Socks5Parser.GetUdpData(info.Data);

            try
            {
                ConnectionKeyUdp key = new ConnectionKeyUdp((info.Tag as ConnectionInfo).Id, info.SourceEP);
                if (udpConnections.TryGetValue(key, out UdpToken token) == false)
                {
                    info.TargetEP = remoteEndPoint;
                    Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    token = new UdpToken { Data = info, TargetSocket = socket, };
                    token.PoolBuffer = new byte[65535];
                    udpConnections.AddOrUpdate(key, token, (a, b) => token);

                    _ = token.TargetSocket.SendTo(sendData.Span, SocketFlags.None, remoteEndPoint);
                    token.Data.Data = Helper.EmptyArray;
                    IAsyncResult result = socket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
                }
                else
                {
                    _ = token.TargetSocket.SendTo(sendData.Span, SocketFlags.None, remoteEndPoint);
                    token.Data.Data = Helper.EmptyArray;
                }
                token.Update();
            }
            catch (Exception ex)
            {
                // Logger.Instance.DebugError($"socks5 forward udp -> sendto {remoteEndPoint} : {sendData.Length}  " + ex);
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

                    var tokens = udpConnections.Where(c => time - c.Value.LastTime > (5 * 60 * 1000));
                    foreach (var item in tokens)
                    {
                        item.Value.Clear();
                        udpConnections.TryRemove(item.Key, out _);
                    }
                }
            }, 1000, true);
        }
        private void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                UdpToken token = result.AsyncState as UdpToken;

                int length = token.TargetSocket.EndReceiveFrom(result, ref token.TempRemoteEP);
                if (length > 0)
                {
                    token.Data.Data = token.PoolBuffer.AsMemory(0, length);

                    token.Update();
                    OnSendResponse(token.Data);
                    token.Data.Data = Helper.EmptyArray;
                }
                result = token.TargetSocket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
            }
            catch (Exception)
            {
            }
        }

        private void HandleCommand(Socks5Info info)
        {
            Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)info.Data.Span[1];
            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(info.Data, out Span<byte> ipMemory);
            if (remoteEndPoint == null)
            {
                ConnectReponse(info, Socks5EnumResponseCommand.NetworkError);
                return;
            }

            if (command == Socks5EnumRequestCommand.Connect)
            {
                Connect(info, remoteEndPoint);
            }
            else if (command == Socks5EnumRequestCommand.UdpAssociate)
            {
                ConnectReponse(info, Socks5EnumResponseCommand.ConnecSuccess);
            }
            else if (command == Socks5EnumRequestCommand.Bind)
            {
                ConnectReponse(info, Socks5EnumResponseCommand.CommandNotAllow);
            }
            else
            {
                ConnectReponse(info, Socks5EnumResponseCommand.CommandNotAllow);
            }
        }
        private void Connect(Socks5Info info, IPEndPoint remoteEndPoint)
        {
            Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = socket,
                Data = info,
                Key = new ConnectionKey((info.Tag as ConnectionInfo).Id, info.Id)
            };
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            connectEventArgs.RemoteEndPoint = remoteEndPoint;
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
        private void TargetProcessConnect(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
            Socks5EnumResponseCommand command = Socks5EnumResponseCommand.ServerError;
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    ConnectReponse(token, Socks5EnumResponseCommand.ConnecSuccess);
                    token.Data.Socks5Step = Socks5EnumStep.Forward;
                    BindTargetReceive(token);
                    return;
                }
                else
                {
                    if (e.SocketError == SocketError.ConnectionRefused)
                    {
                        command = Socks5EnumResponseCommand.DistReject;
                    }
                    else if (e.SocketError == SocketError.NetworkDown)
                    {
                        command = Socks5EnumResponseCommand.NetworkError;
                    }
                    else if (e.SocketError == SocketError.ConnectionReset)
                    {
                        command = Socks5EnumResponseCommand.DistReject;
                    }
                    else if (e.SocketError == SocketError.AddressFamilyNotSupported || e.SocketError == SocketError.OperationNotSupported)
                    {
                        command = Socks5EnumResponseCommand.AddressNotAllow;
                    }
                    else
                    {
                        command = Socks5EnumResponseCommand.ServerError;
                    }
                    ConnectReponse(token, command);
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                //Logger.Instance.DebugError(ex);
                command = Socks5EnumResponseCommand.ServerError;
                ConnectReponse(token, command);
                CloseClientSocket(token);
            }
        }
        private void ConnectReponse(AsyncServerUserToken token, Socks5EnumResponseCommand command)
        {
            ConnectReponse(token.Data, command);
        }
        private void ConnectReponse(Socks5Info data, Socks5EnumResponseCommand command)
        {
            data.Response[0] = (byte)command;
            data.Data = data.Response;
            OnSendResponse(data);
        }

        private void BindTargetReceive(AsyncServerUserToken connectToken)
        {
            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = connectToken.TargetSocket,
                Key = connectToken.Key,
                Data = connectToken.Data
            };
            try
            {
                connections.TryAdd(token.Key, token);
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None,
                };
                token.PoolBuffer = new byte[8*1024];
                readEventArgs.SetBuffer(token.PoolBuffer, 0, 8*1024);
                readEventArgs.Completed += Target_IO_Completed;
                if (token.TargetSocket.ReceiveAsync(readEventArgs) == false)
                {
                    TargetProcessReceive(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                OnSendClose(token.Data);
            }
        }
        private void TargetProcessReceive(SocketAsyncEventArgs e)
        {

            try
            {
                AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    token.Data.Data = e.Buffer.AsMemory(offset, length);
                    OnSendResponse(token.Data);
                    token.Data.Data = Helper.EmptyArray;

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                token.Data.Data = e.Buffer.AsMemory(0, length);
                                OnSendResponse(token.Data);
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
               // Logger.Instance.DebugError(ex);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = e.UserToken as AsyncServerUserToken;
            if (token.IsClosed == false)
            {
                token.Clear();

                e.Dispose();

                connections.TryRemove(token.Key, out _);
                OnSendClose(token.Data);
            }
        }
        private void CloseClientSocket(AsyncServerUserToken token)
        {
            token.IsClosed = true;
            token.Clear();
            connections.TryRemove(token.Key, out _);
        }
    }

    public class AsyncServerUserToken
    {
        public ConnectionKey Key { get; set; }
        public Socket TargetSocket { get; set; }
        public Socks5Info Data { get; set; }
        public bool IsClosed { get; set; } = false;
        public byte[] PoolBuffer { get; set; }
        public void Clear()
        {
            TargetSocket?.SafeClose();
            TargetSocket = null;

            PoolBuffer = Helper.EmptyArray;
            GC.Collect();
        }
    }
    public class ConnectionKeyComparer : IEqualityComparer<ConnectionKey>
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

    public class UdpToken
    {
        public Socket TargetSocket { get; set; }
        public Socks5Info Data { get; set; }
        public byte[] PoolBuffer { get; set; }
        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();
        public EndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        public void Clear()
        {
            TargetSocket?.SafeClose();
            PoolBuffer = Helper.EmptyArray;
        }
        public void Update()
        {
            LastTime = DateTimeHelper.GetTimeStamp();
        }
    }
    public class ConnectionKeyUdpComparer : IEqualityComparer<ConnectionKeyUdp>
    {
        public bool Equals(ConnectionKeyUdp x, ConnectionKeyUdp y)
        {
            return x.Endpoint.Equals(y.Endpoint) && x.ConnectId == y.ConnectId;
        }
        public int GetHashCode(ConnectionKeyUdp obj)
        {
            return obj.Endpoint.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    public readonly struct ConnectionKeyUdp
    {
        public readonly IPEndPoint Endpoint { get; }
        public readonly ulong ConnectId { get; }
        public ConnectionKeyUdp(ulong connectId, IPEndPoint endpoint)
        {
            ConnectId = connectId;
            Endpoint = endpoint;
        }
    }
}
