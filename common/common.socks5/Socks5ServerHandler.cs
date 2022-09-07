using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace common.socks5
{
    public class Socks5ServerHandler : ISocks5ServerHandler
    {
        private ConcurrentDictionary<ConnectionKey, AsyncServerUserToken> connections = new(new ConnectionKeyComparer());
        private ConcurrentDictionary<ConnectionKeyUdp, UdpToken> udpConnections = new(new ConnectionKeyUdpComparer());
        private readonly Dictionary<Socks5EnumStep, Action<IConnection>> handles = new Dictionary<Socks5EnumStep, Action<IConnection>>();

        private readonly ISocks5MessengerSender socks5MessengerSender;
        protected Config Config { get; }
        private readonly WheelTimer<object> wheelTimer;

        Semaphore maxNumberAcceptedClients;
        public Socks5ServerHandler(ISocks5MessengerSender socks5MessengerSender, Config config, WheelTimer<object> wheelTimer)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.Config = config;
            maxNumberAcceptedClients = new Semaphore(config.NumConnections, config.NumConnections);

            this.wheelTimer = wheelTimer;
            TimeoutUdp();

            handles = new Dictionary<Socks5EnumStep, Action<IConnection>> {
                {Socks5EnumStep.Request, HandleRequest},
                {Socks5EnumStep.Auth, HandleAuth},
                {Socks5EnumStep.Command, HandleCommand},
                {Socks5EnumStep.Forward, HndleForward},
                {Socks5EnumStep.ForwardUdp, HndleForwardUdp},
            };
        }

        public void InputData(IConnection connection)
        {
            Socks5EnumStep step = (Socks5EnumStep)(byte)(connection.ReceiveRequestWrap.Memory.Span[0] >> 4);
            if (handles.TryGetValue(step, out Action<IConnection> action))
            {
                action(connection);
            }
        }

        private void HandleRequest(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Memory);
            if (!Config.ConnectEnable)
            {
                data.Response[0] = (byte)Socks5EnumAuthType.NotSupported;
            }
            else
            {
                data.Response[0] = (byte)Socks5EnumAuthType.NoAuth;
            }
            data.Data = data.Response;
            socks5MessengerSender.Response(data, connection);
        }
        private void HandleAuth(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Memory);
            if (!Config.ConnectEnable)
            {
                data.Response[0] = (byte)Socks5EnumAuthState.UnKnow;
            }
            else
            {
                data.Response[0] = (byte)Socks5EnumAuthState.Success;
            }
            data.Data = data.Response;
            socks5MessengerSender.Response(data, connection);
        }
        private void HndleForward(IConnection connection)
        {
            if (Config.ConnectEnable)
            {
                Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Memory);
                ConnectionKey key = new ConnectionKey(connection.ConnectId, data.Id);
                if (connections.TryGetValue(key, out AsyncServerUserToken token))
                {
                    if (data.Data.Length > 0)
                    {
                        _ = token.TargetSocket.SendAsync(data.Data, SocketFlags.None);
                    }
                    else
                    {
                        CloseClientSocket(token);
                    }
                }
            }
        }
        private void HndleForwardUdp(IConnection connection)
        {
            if (Config.ConnectEnable)
            {
                Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Memory);

                IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(data.Data);
                Memory<byte> sendData = Socks5Parser.GetUdpData(data.Data);

                ConnectionKeyUdp key = new ConnectionKeyUdp(connection.ConnectId, data.SourceEP);
                if (!udpConnections.TryGetValue(key, out UdpToken token))
                {
                    Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    token = new UdpToken { Connection = connection, Data = data, TargetSocket = socket, };
                    token.PoolBuffer = ArrayPool<byte>.Shared.Rent(65535);
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
                    socks5MessengerSender.Response(token.Data, token.Connection);
                    token.Data.Data = Helper.EmptyArray;
                }
                result = token.TargetSocket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
            }
            catch (Exception)
            {
            }
        }

        private void HandleCommand(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Memory);
            if (!Config.ConnectEnable)
            {
                ConnectReponse(data, Socks5EnumResponseCommand.ConnectNotAllow, connection);
            }
            else
            {
                Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)data.Data.Span[1];
                if (command == Socks5EnumRequestCommand.Connect)
                {
                    IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(data.Data);
                    if (remoteEndPoint == null)
                    {
                        ConnectReponse(data, Socks5EnumResponseCommand.AddressNotAllow, connection);
                    }
                    else if (!Config.LanConnectEnable && remoteEndPoint.IsLan())
                    {
                        ConnectReponse(data, Socks5EnumResponseCommand.AddressNotAllow, connection);
                    }
                    else
                    {
                        Connect(connection, data, remoteEndPoint);
                    }
                }
                else if (command == Socks5EnumRequestCommand.UdpAssociate)
                {
                    ConnectReponse(data, Socks5EnumResponseCommand.ConnecSuccess, connection);
                }
                else if (command == Socks5EnumRequestCommand.Bind)
                {
                    ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow, connection);
                }
                else
                {
                    ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow, connection);
                }
            }
        }
        private void Connect(IConnection connection, Socks5Info data, IPEndPoint remoteEndPoint)
        {
            maxNumberAcceptedClients.WaitOne();
            Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            AsyncServerUserToken token = new AsyncServerUserToken
            {
                Connection = connection,
                TargetSocket = socket,
                Data = data,
                Key = new ConnectionKey(connection.ConnectId, data.Id)
            };
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            connectEventArgs.RemoteEndPoint = remoteEndPoint;
            connectEventArgs.Completed += Target_IO_Completed;
            if (!socket.ConnectAsync(connectEventArgs))
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
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                command = Socks5EnumResponseCommand.ServerError;
                CloseClientSocket(token);
            }
            ConnectReponse(token, command);
        }
        private void ConnectReponse(AsyncServerUserToken token, Socks5EnumResponseCommand command)
        {
            ConnectReponse(token.Data, command, token.Connection);
        }
        private void ConnectReponse(Socks5Info data, Socks5EnumResponseCommand command, IConnection connection)
        {
            data.Response[0] = (byte)command;
            data.Data = data.Response;
            socks5MessengerSender.Response(data, connection);
        }

        private void BindTargetReceive(AsyncServerUserToken connectToken)
        {
            AsyncServerUserToken token = new AsyncServerUserToken
            {
                Connection = connectToken.Connection,
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
                token.PoolBuffer = ArrayPool<byte>.Shared.Rent(Config.BufferSize);
                readEventArgs.SetBuffer(token.PoolBuffer, 0, Config.BufferSize);
                readEventArgs.Completed += Target_IO_Completed;
                if (!token.TargetSocket.ReceiveAsync(readEventArgs))
                {
                    TargetProcessReceive(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                socks5MessengerSender.ResponseClose(token.Key.RequestId, token.Connection);
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
                    socks5MessengerSender.Response(token.Data, token.Connection);
                    token.Data.Data = Helper.EmptyArray;

                    if (token.TargetSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(Config.BufferSize);
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(arr);
                            if (length > 0)
                            {
                                token.Data.Data = arr.AsMemory(0, length);
                                socks5MessengerSender.Response(token.Data, token.Connection);
                                token.Data.Data = Helper.EmptyArray;
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (!token.TargetSocket.Connected)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (!token.TargetSocket.ReceiveAsync(e))
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

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = e.UserToken as AsyncServerUserToken;
            if (!token.IsClosed)
            {
                IConnection connection = token.Connection;
                token.Clear();
                connections.TryRemove(token.Key, out _);
                e.Dispose();

                maxNumberAcceptedClients.Release();
                socks5MessengerSender.ResponseClose(token.Key.RequestId, connection);
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
        public IConnection Connection { get; set; }
        public Socket TargetSocket { get; set; }
        public Socks5Info Data { get; set; }
        public bool IsClosed { get; set; } = false;

        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            TargetSocket?.SafeClose();
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }
            GC.Collect();
            GC.SuppressFinalize(this);
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
        public readonly ulong RequestId { get; }
        public readonly ulong ConnectId { get; }

        public ConnectionKey(ulong connectId, ulong requestId)
        {
            ConnectId = connectId;
            RequestId = requestId;
        }
    }

    public class UdpToken
    {
        public IConnection Connection { get; set; }
        public Socket TargetSocket { get; set; }
        public Socks5Info Data { get; set; }
        public byte[] PoolBuffer { get; set; }

        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();
        public EndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

        public void Clear()
        {
            TargetSocket?.SafeClose();
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }
            GC.Collect();
            GC.SuppressFinalize(this);
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
