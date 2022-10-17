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
            if (arg.DataType == TcpForwardDataTypes.FORWARD)
            {
                ConnectionKey key = new ConnectionKey(arg.Connection.FromConnection.ConnectId, arg.RequestId);
                if (connections.TryGetValue(key, out ConnectUserToken token) && token.TargetSocket != null && token.TargetSocket.Connected)
                {
                    token.SendArg.DataType = arg.DataType;
                    if (arg.Buffer.Length > 0)
                    {
                        token.TargetSocket.Send(arg.Buffer.Span, SocketFlags.None);
                    }
                    else
                    {
                        token.Clear();
                        connections.TryRemove(token.Key, out _);
                    }
                }
            }
            else
            {
                Connect(arg);
            }
        }

        private void Connect(TcpForwardInfo arg)
        {
            if (tcpForwardValidator.Validate(arg) == false)
            {
                arg.StateType = TcpForwardStateTypes.Fail;
                arg.Buffer = Helper.EmptyArray;
                tcpForwardMessengerSender.SendResponse(arg);
                return;
            }

            IPEndPoint endpoint = NetworkHelper.EndpointFromArray(arg.TargetEndpoint);

            //maxNumberAcceptedClients.WaitOne();
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

                    token.SendArg.StateType = TcpForwardStateTypes.Success;
                    token.SendArg.Buffer = Helper.EmptyArray;
                    tcpForwardMessengerSender.SendResponse(token.SendArg);


                    token.PoolBuffer = new byte[config.BufferSize];//ConnectUserToken.arrayPool.Rent(config.BufferSize);
                    e.SetBuffer(token.PoolBuffer, 0, config.BufferSize);

                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    Console.WriteLine($"{token.SendArg.TargetEndpoint.GetString()} fail:{e.SocketError}");
                    token.SendArg.StateType = TcpForwardStateTypes.Fail;
                    token.SendArg.Buffer = Helper.EmptyArray;
                    tcpForwardMessengerSender.SendResponse(token.SendArg);
                    token.Clear();
                    connections.TryRemove(token.Key, out _);
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
            ConnectUserToken token = (ConnectUserToken)e.UserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {

                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    token.SendArg.Buffer = e.Buffer.AsMemory(offset, length);
                    tcpForwardMessengerSender.SendResponse(token.SendArg);
                    token.SendArg.Buffer = Helper.EmptyArray;

                    if (token.TargetSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(config.BufferSize);
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(arr);
                            if (length > 0)
                            {
                                token.SendArg.Buffer = arr.AsMemory(0, length);
                                tcpForwardMessengerSender.SendResponse(token.SendArg);
                                token.SendArg.Buffer = Helper.EmptyArray;
                            }
                        }

                        ArrayPool<byte>.Shared.Return(arr);
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
                CloseClientSocket(token);
            }
        }

        private void CloseClientSocket(ConnectUserToken token)
        {
            //maxNumberAcceptedClients.Release();
            token.SendArg.StateType = TcpForwardStateTypes.Close;
            token.SendArg.Buffer = Helper.EmptyArray;
            tcpForwardMessengerSender.SendResponse(token.SendArg);
            token.Clear();
            connections.TryRemove(token.Key, out _);
        }
    }

    public class ConnectUserToken
    {
        public static ArrayPool<byte> arrayPool = ArrayPool<byte>.Create();
        public Socket TargetSocket { get; set; }
        public ConnectionKey Key { get; set; }
        public TcpForwardInfo SendArg { get; set; }
        public bool Receive { get; set; } = false;
        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                //arrayPool.Return(PoolBuffer,true);
            }

            TargetSocket?.SafeClose();
            TargetSocket = null;

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