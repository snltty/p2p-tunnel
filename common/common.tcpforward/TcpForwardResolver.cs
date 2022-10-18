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
                   // Console.WriteLine($"收到数据：{arg.DataType}：{arg.StateType}：{arg.Buffer.Length}：{arg.RequestId}");
                    if (connections.TryGetValue(key, out ConnectUserToken token))
                    {
                        if (arg.Buffer.Length > 0)
                        {
                            try
                            {
                                //Console.WriteLine($"收到数据1：{arg.DataType}：{arg.StateType}：{arg.Buffer.Length}：{arg.RequestId}");
                                token.TargetSocket.Send(arg.Buffer.Span, SocketFlags.None);
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
                    //Console.WriteLine($"收到连接：{arg.DataType}：{arg.StateType}：{arg.Buffer.Length}：{arg.RequestId}");
                    Connect(arg);
                }
            }
            else
            {
                //Console.WriteLine($"收到关闭：{arg.DataType}：{arg.StateType}：{arg.Buffer.Length}：{arg.RequestId}");
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
            ConnectUserToken connectToken = (ConnectUserToken)e.UserToken;
            ConnectUserToken token = new ConnectUserToken
            {
                TargetSocket = connectToken.TargetSocket,
                Key = connectToken.Key,
                SendArg = connectToken.SendArg,
            };

            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    connections.TryAdd(token.Key, token);

                    SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                    {
                        UserToken = token,
                        SocketFlags = SocketFlags.None,
                    };
                    token.SendArg.TargetEndpoint = Helper.EmptyArray;
                    token.SendArg.StateType = TcpForwardStateTypes.Success;
                    Receive(token.SendArg, Helper.EmptyArray);

                    token.PoolBuffer = new byte[config.BufferSize];
                    readEventArgs.SetBuffer(token.PoolBuffer, 0, config.BufferSize);
                    readEventArgs.Completed += IO_Completed;

                    if (token.TargetSocket.ReceiveAsync(readEventArgs) == false)
                    {
                        ProcessReceive(readEventArgs);
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
                    //Console.WriteLine($"异步接收数据:{e.BytesTransferred}:{token.SendArg.RequestId}");

                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    Receive(token.SendArg, e.Buffer.AsMemory(offset, length));
                   // Console.WriteLine($"回复异步接收数据:{res}:{e.BytesTransferred}:{token.SendArg.RequestId}");

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                           // Console.WriteLine($"同步接收数据:{token.SendArg.RequestId}");
                            length = token.TargetSocket.Receive(e.Buffer);
                            //Console.WriteLine($"同步接收数据:{length}:{token.SendArg.RequestId}");
                            if (length > 0)
                            {
                                Receive(token.SendArg, e.Buffer.AsMemory(0, length));
                               // Console.WriteLine($"回复同步接收数据:{res}:{length}:{token.SendArg.RequestId}");
                            }
                        }
                    }
                    if (token.TargetSocket.Connected == false)
                    {
                       // Console.WriteLine($"关闭接收数据:{token.SendArg.RequestId}");
                        CloseClientSocket(token);
                        return;
                    }
                  //  Console.WriteLine($"继续接收数据:{token.SendArg.RequestId}");
                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                  //  Console.WriteLine($"关闭接收数据1:{token.SendArg.RequestId}:{e.BytesTransferred}:{e.SocketError}");
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
                //Console.WriteLine($"发送关闭数据包:{token.SendArg.RequestId}");
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