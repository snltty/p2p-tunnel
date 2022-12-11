using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public class Socks5ServerHandler : ISocks5ServerHandler
    {
        private ConcurrentDictionary<ConnectionKey, AsyncServerUserToken> connections = new(new ConnectionKeyComparer());
        private ConcurrentDictionary<ConnectionKeyUdp, UdpToken> udpConnections = new(new ConnectionKeyUdpComparer());
        private readonly Dictionary<Socks5EnumStep, Action<IConnection>> handles = new Dictionary<Socks5EnumStep, Action<IConnection>>();

        private readonly ISocks5MessengerSender socks5MessengerSender;
        /// <summary>
        /// 
        /// </summary>
        protected Config config { get; }
        private readonly WheelTimer<object> wheelTimer;
        private readonly ISocks5Validator socks5Validator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socks5MessengerSender"></param>
        /// <param name="config"></param>
        /// <param name="wheelTimer"></param>
        /// <param name="socks5Validator"></param>
        public Socks5ServerHandler(ISocks5MessengerSender socks5MessengerSender, Config config, WheelTimer<object> wheelTimer, ISocks5Validator socks5Validator)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.config = config;

            this.wheelTimer = wheelTimer;
            this.socks5Validator = socks5Validator;
            TimeoutUdp();

            handles = new Dictionary<Socks5EnumStep, Action<IConnection>> {
                {Socks5EnumStep.Request, HandleRequest},
                {Socks5EnumStep.Auth, HandleAuth},
                {Socks5EnumStep.Command, HandleCommand},
                {Socks5EnumStep.Forward, HndleForward},
                {Socks5EnumStep.ForwardUdp, HndleForwardUdp},
            };

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public void InputData(IConnection connection)
        {
            Socks5EnumStep step = (Socks5EnumStep)(byte)(connection.ReceiveRequestWrap.Payload.Span[0] >> 4);
            if (handles.TryGetValue(step, out Action<IConnection> action))
            {
                action(connection);
            }
        }

        private void HandleRequest(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            data.Response[0] = (byte)Socks5EnumAuthType.NoAuth;
            data.Data = data.Response;
            socks5MessengerSender.Response(data, connection.FromConnection);
        }
        private void HandleAuth(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            data.Response[0] = (byte)Socks5EnumAuthState.Success;
            data.Data = data.Response;
            socks5MessengerSender.Response(data, connection.FromConnection);
        }
        private void HndleForward(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            ConnectionKey key = new ConnectionKey(connection.FromConnection.ConnectId, data.Id);
            if (connections.TryGetValue(key, out AsyncServerUserToken token))
            {
                if (data.Data.Length > 0 && token.TargetSocket.Connected)
                {
                    try
                    {
                        _ = token.TargetSocket.Send(data.Data.Span, SocketFlags.None);
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
        private void HndleForwardUdp(IConnection connection)
        {
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);

            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(data.Data, out Span<byte> ipMemory);
            Memory<byte> sendData = Socks5Parser.GetUdpData(data.Data);

            ConnectionKeyUdp key = new ConnectionKeyUdp(connection.FromConnection.ConnectId, data.SourceEP);
            if (udpConnections.TryGetValue(key, out UdpToken token) == false)
            {
                data.TargetEP = remoteEndPoint;
                Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                token = new UdpToken { Connection = connection.FromConnection, Data = data, TargetSocket = socket, };
                token.PoolBuffer = new byte[65535];
                udpConnections.AddOrUpdate(key, token, (a, b) => token);

                try
                {
                    _ = token.TargetSocket.SendTo(sendData.Span, SocketFlags.None, remoteEndPoint);
                    token.Data.Data = Helper.EmptyArray;
                    IAsyncResult result = socket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                _ = token.TargetSocket.SendTo(sendData.Span, SocketFlags.None, remoteEndPoint);
                token.Data.Data = Helper.EmptyArray;
            }
            token.Update();
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
            Socks5Info data = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);

            if (socks5Validator.Validate(connection.FromConnection, data, config) == false)
            {
                ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow, connection.FromConnection);
                return;
            }

            Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)data.Data.Span[1];
            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(data.Data, out Span<byte> ipMemory);

            if (command == Socks5EnumRequestCommand.Connect)
            {
                Connect(connection.FromConnection, data, remoteEndPoint);
            }
            else if (command == Socks5EnumRequestCommand.UdpAssociate)
            {
                ConnectReponse(data, Socks5EnumResponseCommand.ConnecSuccess, connection.FromConnection);
            }
            else if (command == Socks5EnumRequestCommand.Bind)
            {
                ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow, connection.FromConnection);
            }
            else
            {
                ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow, connection.FromConnection);
            }
        }
        private void Connect(IConnection connection, Socks5Info data, IPEndPoint remoteEndPoint)
        {
            //maxNumberAcceptedClients.WaitOne();
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
                Logger.Instance.DebugError(ex);
                command = Socks5EnumResponseCommand.ServerError;
                ConnectReponse(token, command);
                CloseClientSocket(token);
            }
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
                token.PoolBuffer = new byte[config.BufferSize];
                readEventArgs.SetBuffer(token.PoolBuffer, 0, config.BufferSize);
                readEventArgs.Completed += Target_IO_Completed;
                if (token.TargetSocket.ReceiveAsync(readEventArgs) == false)
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
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                token.Data.Data = e.Buffer.AsMemory(0, length);
                                socks5MessengerSender.Response(token.Data, token.Connection);
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

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = e.UserToken as AsyncServerUserToken;
            if (token.IsClosed == false)
            {
                IConnection connection = token.Connection;
                token.Clear();

                e.Dispose();

                connections.TryRemove(token.Key, out _);
                //maxNumberAcceptedClients.Release();
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

    /// <summary>
    /// 
    /// </summary>
    public class AsyncServerUserToken
    {
        /// <summary>
        /// 
        /// </summary>
        public ConnectionKey Key { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socket TargetSocket { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5Info Data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsClosed { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public byte[] PoolBuffer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            TargetSocket?.SafeClose();
            TargetSocket = null;

            PoolBuffer = Helper.EmptyArray;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionKeyComparer : IEqualityComparer<ConnectionKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ConnectionKey x, ConnectionKey y)
        {
            return x.RequestId == y.RequestId && x.ConnectId == y.ConnectId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(ConnectionKey obj)
        {
            return obj.RequestId.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public readonly struct ConnectionKey
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly uint RequestId { get; }
        /// <summary>
        /// 
        /// </summary>
        public readonly ulong ConnectId { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectId"></param>
        /// <param name="requestId"></param>
        public ConnectionKey(ulong connectId, uint requestId)
        {
            ConnectId = connectId;
            RequestId = requestId;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class UdpToken
    {
        /// <summary>
        /// 
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socket TargetSocket { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5Info Data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] PoolBuffer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();
        /// <summary>
        /// 
        /// </summary>
        public EndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            TargetSocket?.SafeClose();
            PoolBuffer = Helper.EmptyArray;
            //GC.Collect();
            // GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Update()
        {
            LastTime = DateTimeHelper.GetTimeStamp();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionKeyUdpComparer : IEqualityComparer<ConnectionKeyUdp>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ConnectionKeyUdp x, ConnectionKeyUdp y)
        {
            return x.Endpoint.Equals(y.Endpoint) && x.ConnectId == y.ConnectId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(ConnectionKeyUdp obj)
        {
            return obj.Endpoint.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public readonly struct ConnectionKeyUdp
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly IPEndPoint Endpoint { get; }
        /// <summary>
        /// 
        /// </summary>
        public readonly ulong ConnectId { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectId"></param>
        /// <param name="endpoint"></param>
        public ConnectionKeyUdp(ulong connectId, IPEndPoint endpoint)
        {
            ConnectId = connectId;
            Endpoint = endpoint;
        }
    }
}
