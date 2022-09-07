using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace common.tcpforward
{
    public class TcpForwardResolver
    {
        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        private readonly Config config;
        private ConcurrentDictionary<ConnectionKey, ConnectUserToken> connections = new ConcurrentDictionary<ConnectionKey, ConnectUserToken>(new ConnectionComparer());

        Semaphore maxNumberAcceptedClients;

        public TcpForwardResolver(TcpForwardMessengerSender tcpForwardMessengerSender, Config config)
        {
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.config = config;

            //B接收到A的请求
            tcpForwardMessengerSender.OnRequestHandler.Sub(OnRequest);

            maxNumberAcceptedClients = new Semaphore(config.NumConnections, config.NumConnections);
        }

        private void OnRequest(TcpForwardInfo arg)
        {
            ConnectionKey key = new ConnectionKey(arg.Connection.ConnectId, arg.RequestId);
            if (connections.TryGetValue(key, out ConnectUserToken token) && token.TargetSocket != null && token.TargetSocket.Connected)
            {
                if (arg.Buffer.Length > 0)
                {
                    token.TargetSocket.Send(arg.Buffer.Span);
                }
                else
                {
                    token.Clear();
                    connections.TryRemove(token.Key, out _);
                }
            }
            else if (arg.TargetEndpoint.Length > 0)
            {
                Connect(arg);
            }
        }
        private void Connect(TcpForwardInfo arg)
        {
           
            IPEndPoint endpoint = NetworkHelper.EndpointFromArray(arg.TargetEndpoint);
            if (!config.LanConnectEnable && arg.ForwardType == TcpForwardTypes.PROXY && endpoint.IsLan())
            {
                Receive(arg, Helper.EmptyArray, 0, 0);
                return;
            }
            if (!Intercept(arg, endpoint.Port))
            {
                Receive(arg, Helper.EmptyArray, 0, 0);
                return;
            }

            maxNumberAcceptedClients.WaitOne();
            Socket socket = new(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = endpoint;
            saea.Completed += IO_Completed;

            saea.UserToken = new ConnectUserToken
            {
                Key = new ConnectionKey(arg.Connection.ConnectId, arg.RequestId),
                SendArg = arg
            };

            if (!socket.ConnectAsync(saea))
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
                    if (token.SendArg.ForwardType == TcpForwardTypes.PROXY)
                    {
                        Receive(token, HttpConnectMethodHelper.ConnectSuccessMessage());
                    }
                    if (token.SendArg.Buffer.Length > 0 && token.SendArg.ForwardType != TcpForwardTypes.PROXY)
                    {
                        e.ConnectSocket.Send(token.SendArg.Buffer.Span, SocketFlags.None);
                    }
                    token.SendArg.Buffer = Helper.EmptyArray;

                    token.TargetSocket = e.ConnectSocket;
                    token.Buffer = ArrayPool<byte>.Shared.Rent(config.BufferSize);
                    e.SetBuffer(token.Buffer, 0, config.BufferSize);
                    connections.TryAdd(token.Key, token);
                    if (!token.TargetSocket.ReceiveAsync(e))
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    if (token.SendArg.ForwardType == TcpForwardTypes.PROXY)
                    {
                        Receive(token, HttpConnectMethodHelper.ConnectErrorMessage());
                    }
                    CloseClientSocket(token);
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
                    Receive(token, e.Buffer, 0, e.BytesTransferred);

                    if (token.TargetSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(config.BufferSize);
                        while (token.TargetSocket.Available > 0)
                        {
                            int length = token.TargetSocket.Receive(arr);
                            if (length > 0)
                            {
                                Receive(token, arr, 0, length);
                            }
                        }

                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (!token.TargetSocket.ReceiveAsync(e))
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
            maxNumberAcceptedClients.Release();
            Receive(token, Helper.EmptyArray, 0, 0);
            token.Clear();
            connections.TryRemove(token.Key, out _);
        }

        private bool Intercept(TcpForwardInfo arg, int port)
        {
            if (!config.ConnectEnable)
            {
                return false;
            }
            if (config.PortWhiteList.Length > 0 && !config.PortWhiteList.Contains(port))
            {
                return false;
            }
            if (config.PortBlackList.Contains(port))
            {
                return false;
            }
            return true;
        }

        private void Receive(ConnectUserToken token, byte[] data)
        {
            Receive(token.SendArg, data, 0, data.Length);
        }
        private void Receive(ConnectUserToken token, byte[] data, int offset, int length)
        {
            Receive(token.SendArg, data, offset, length);
        }
        private void Receive(TcpForwardInfo arg, byte[] data, int offset, int length)
        {
            arg.Buffer = data.AsMemory(offset, length);
            tcpForwardMessengerSender.SendResponse(arg).ConfigureAwait(false).GetAwaiter().GetResult();
        }

    }

    public class ConnectUserToken
    {
        public Socket TargetSocket { get; set; }
        public ConnectionKey Key { get; set; }
        public TcpForwardInfo SendArg { get; set; }

        public byte[] Buffer { get; set; }

        public void Clear()
        {
            if (Buffer != null && Buffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(Buffer);
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