using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace common.tcpforward
{
    public class TcpForwardResolver
    {
        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        private readonly Config config;
        private readonly ITcpForwardValidator tcpForwardValidator;
        private ConcurrentDictionary<ConnectionKey, ConnectUserToken> connections = new ConcurrentDictionary<ConnectionKey, ConnectUserToken>(new ConnectionComparer());

        Semaphore maxNumberAcceptedClients;

        public TcpForwardResolver(TcpForwardMessengerSender tcpForwardMessengerSender, Config config, ITcpForwardValidator tcpForwardValidator)
        {
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.config = config;

            maxNumberAcceptedClients = new Semaphore(config.NumConnections, config.NumConnections);
            this.tcpForwardValidator = tcpForwardValidator;
        }
        public void InputData(IConnection connection)
        {
            TcpForwardInfo data = new TcpForwardInfo();
            data.Connection = connection;
            data.DeBytes(connection.ReceiveRequestWrap.Memory);
            OnRequest(data);
        }

        private void OnRequest(TcpForwardInfo arg)
        {
            ConnectionKey key = new ConnectionKey(arg.Connection.FromConnection.ConnectId, arg.RequestId);
            if (arg.StateType == TcpForwardStateTypes.Success)
            {
                if (arg.DataType == TcpForwardDataTypes.FORWARD)
                {
                    if (connections.TryGetValue(key, out ConnectUserToken token))
                    {
                        if (arg.Buffer.Length > 0)
                        {
                            try
                            {
                                token.TargetSocket.SendAsync(arg.Buffer, SocketFlags.None);
                                return;
                            }
                            catch (Exception)
                            {
                                CloseClientSocket(token);
                                return;
                            }
                        }
                    }

                    arg.StateType = TcpForwardStateTypes.Close;
                    Receive(arg, Helper.EmptyArray);

                }
                else if (arg.DataType == TcpForwardDataTypes.CONNECT)
                {
                    Connect(arg);
                }
            }
            else
            {
                if (connections.TryRemove(key, out ConnectUserToken token))
                {
                    CloseClientSocket(token);
                }
            }
        }

        private void Connect(TcpForwardInfo arg)
        {
            if (tcpForwardValidator.Validate(arg) == false)
            {
                arg.StateType = TcpForwardStateTypes.Fail;
                Receive(arg, Helper.EmptyArray);
                return;
            }

            IPEndPoint endpoint = NetworkHelper.EndpointFromArray(arg.TargetEndpoint);

            // maxNumberAcceptedClients.WaitOne();
            Socket socket = new(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = endpoint;
            saea.Completed += IO_Completed;
            saea.UserToken = new ConnectUserToken
            {
                Key = new ConnectionKey(arg.Connection.FromConnection.ConnectId, arg.RequestId),
                TargetSocket = socket,
                SendArg = arg
            };
            if (socket.ConnectAsync(saea) == false)
            {
                ProcessConnect(saea);
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    break;
            }
        }
        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            ConnectUserToken token = (ConnectUserToken)e.UserToken;
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    connections.TryAdd(token.Key, token);

                    var buffer = token.SendArg.Buffer;
                    token.SendArg.TargetEndpoint = Helper.EmptyArray;
                    token.SendArg.StateType = TcpForwardStateTypes.Success;
                    Receive(token.SendArg, Helper.EmptyArray);

                    token.PoolBuffer = ArrayPool<byte>.Shared.Rent(config.BufferSize); //new byte[config.BufferSize];
                    e.SetBuffer(token.PoolBuffer, 0, config.BufferSize);

                    if (buffer.Length > 0)
                    {
                        token.TargetSocket.SendAsync(buffer, SocketFlags.None);
                    }

                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(token, TcpForwardStateTypes.Fail);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                CloseClientSocket(token);
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                ConnectUserToken token = e.UserToken as ConnectUserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;

                    Receive(token.SendArg, e.Buffer.AsMemory(offset, length));

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                Receive(token.SendArg, e.Buffer.AsMemory(0, length));
                            }
                        }
                    }
                    if (token.TargetSocket.Connected == false)
                    {
                        CloseClientSocket(token);
                        return;
                    }
                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                CloseClientSocket(e);
            }
        }

        private void Receive(TcpForwardInfo arg, Memory<byte> data)
        {
            arg.Buffer = data;
            tcpForwardMessengerSender.SendResponse(arg);
            arg.Buffer = Helper.EmptyArray;
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            ConnectUserToken token = e.UserToken as ConnectUserToken;
            CloseClientSocket(token);
        }
        private void CloseClientSocket(ConnectUserToken token, TcpForwardStateTypes state = TcpForwardStateTypes.Close)
        {
            if (token != null)
            {
                //maxNumberAcceptedClients.Release();
                token.SendArg.StateType = state;
                Receive(token.SendArg, Helper.EmptyArray);
                token.Clear();
                connections.TryRemove(token.Key, out _);
            }
        }
    }

    public class ConnectUserToken
    {
        public Socket TargetSocket { get; set; }
        public ConnectionKey Key { get; set; }
        public TcpForwardInfo SendArg { get; set; }
        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            TargetSocket?.SafeClose();
            TargetSocket = null;

            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
                PoolBuffer = null;
            }

            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }

    public class ConnectionComparer : IEqualityComparer<ConnectionKey>
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

        public override string ToString()
        {
            return $"{ConnectId},{RequestId}";
        }
    }
}