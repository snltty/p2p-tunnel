using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace common.tcpforward
{
    public class TcpForwardServerPre : ITcpForwardServer
    {
        public SimpleSubPushHandler<TcpForwardInfo> OnRequest { get; } = new SimpleSubPushHandler<TcpForwardInfo>();
        public SimpleSubPushHandler<ListeningChangeInfo> OnListeningChange { get; } = new SimpleSubPushHandler<ListeningChangeInfo>();
        private readonly ServersManager serversManager = new ServersManager();
        private readonly ClientsManager clientsManager = new ClientsManager();

        private NumberSpace requestIdNs = new NumberSpace(0);

        private int receiveBufferSize = 8 * 1024;

        public TcpForwardServerPre()
        {
        }

        public void Init(int numConnections, int receiveBufferSize)
        {
            this.receiveBufferSize = receiveBufferSize;
        }
        public void Start(int port, TcpForwardAliveTypes aliveType)
        {
            if (serversManager.Contains(port))
            {
                return;
            }

            BindAccept(port, aliveType);
            OnListeningChange.Push(new ListeningChangeInfo { Port = port, State = true });
        }

        private void BindAccept(int port, TcpForwardAliveTypes aliveType)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endpoint);
            socket.Listen(int.MaxValue);

            serversManager.TryAdd(new ServerInfo
            {
                SourcePort = port,
                Socket = socket
            });

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            ForwardAsyncUserToken token = new ForwardAsyncUserToken
            {
                SourceSocket = socket,
                SourcePort = port,
            };
            token.Request.AliveType = aliveType;
            token.Request.SourcePort = port;
            acceptEventArg.UserToken = token;
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            try
            {
                acceptEventArg.AcceptSocket = null;
                ForwardAsyncUserToken token = ((ForwardAsyncUserToken)acceptEventArg.UserToken);
                if (token.SourceSocket.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
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
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
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
            ForwardAsyncUserToken acceptToken = (e.UserToken as ForwardAsyncUserToken);

            ulong id = requestIdNs.Increment();
            ForwardAsyncUserToken token = new ForwardAsyncUserToken
            {
                SourceSocket = e.AcceptSocket,
                Request = new TcpForwardInfo
                {
                    RequestId = id,
                    AliveType = acceptToken.Request.AliveType,
                    SourcePort = acceptToken.SourcePort,
                    Buffer = Helper.EmptyArray,
                },
                SourcePort = acceptToken.SourcePort
            };
            clientsManager.TryAdd(token);

            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None
            };

            token.PoolBuffer = ArrayPool<byte>.Shared.Rent(receiveBufferSize);
            readEventArgs.SetBuffer(token.PoolBuffer, 0, receiveBufferSize);
            readEventArgs.Completed += IO_Completed;
            if (token.SourceSocket.ReceiveAsync(readEventArgs) == false)
            {
                ProcessReceive(readEventArgs);
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                ForwardAsyncUserToken token = (ForwardAsyncUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    token.Request.Buffer = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    Receive(token);
                    token.Request.Buffer = Helper.EmptyArray;

                    if (token.SourceSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(token.SourceSocket.Available);
                        while (token.SourceSocket.Available > 0)
                        {
                            int length = token.SourceSocket.Receive(arr);
                            if (length > 0)
                            {
                                token.Request.Buffer = arr.AsMemory(0, length);
                                Receive(token);
                                token.Request.Buffer = Helper.EmptyArray;
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }
                    if (token.SourceSocket.Connected)
                    {
                        if (token.SourceSocket.ReceiveAsync(e) == false)
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
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ForwardAsyncUserToken token = (ForwardAsyncUserToken)e.UserToken;
                if (token.SourceSocket.ReceiveAsync(e) == false)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void Receive(ForwardAsyncUserToken token)
        {
            OnRequest.Push(token.Request);
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            ForwardAsyncUserToken token = e.UserToken as ForwardAsyncUserToken;

            clientsManager.TryRemove(token.Request.RequestId, out _);

            if (token.Request.Connection != null)
            {
                token.Request.Buffer = Helper.EmptyArray;
                OnRequest.Push(token.Request);
            }
        }

        public void Response(TcpForwardInfo model)
        {
            if (clientsManager.TryGetValue(model.RequestId, out ForwardAsyncUserToken token))
            {
                var span = model.Buffer.Span;
                if (span.Length > 0)
                {
                    try
                    {
                        token.SourceSocket.Send(span, SocketFlags.None);
                    }
                    catch (Exception)
                    {
                        clientsManager.TryRemove(model.RequestId, out _);
                    }
                }
                else
                {
                    clientsManager.TryRemove(model.RequestId, out _);
                }
            }
        }

        public void Stop(int sourcePort)
        {
            if (serversManager.TryRemove(sourcePort, out ServerInfo model))
            {
                OnListeningChange.Push(new ListeningChangeInfo
                {
                    Port = model.SourcePort,
                    State = false
                });

                clientsManager.Clear(model.SourcePort);
            }
        }
        public void Stop()
        {
            serversManager.Clear();
            clientsManager.Clear();
            OnListeningChange.Push(new ListeningChangeInfo { Port = 0, State = true });
        }
    }

    public class ForwardAsyncUserToken
    {
        public Socket SourceSocket { get; set; }
        public int SourcePort { get; set; } = 0;
        public TcpForwardInfo Request { get; set; } = new TcpForwardInfo { };
        public byte[] PoolBuffer { get; set; }
    }
    public class ClientsManager
    {
        private ConcurrentDictionary<ulong, ForwardAsyncUserToken> clients = new();

        public bool TryAdd(ForwardAsyncUserToken model)
        {
            return clients.TryAdd(model.Request.RequestId, model);
        }
        public bool TryGetValue(ulong id, out ForwardAsyncUserToken c)
        {
            return clients.TryGetValue(id, out c);
        }
        public bool TryRemove(ulong id, out ForwardAsyncUserToken c)
        {
            bool res = clients.TryRemove(id, out c);
            if (res)
            {
                try
                {
                    ArrayPool<byte>.Shared.Return(c.PoolBuffer);
                    c.SourceSocket.SafeClose();
                    GC.Collect();
                    GC.SuppressFinalize(c);
                }
                catch (Exception)
                {
                }
            }
            return res;
        }
        public void Clear(int sourcePort)
        {
            IEnumerable<ulong> requestIds = clients.Where(c => c.Value.SourcePort == sourcePort).Select(c => c.Key);
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

    public class ServersManager
    {
        public ConcurrentDictionary<int, ServerInfo> services = new();
        public bool TryAdd(ServerInfo model)
        {
            return services.TryAdd(model.SourcePort, model);
        }
        public bool Contains(int port)
        {
            return services.ContainsKey(port);
        }
        public bool TryGetValue(int port, out ServerInfo c)
        {
            return services.TryGetValue(port, out c);
        }
        public bool TryRemove(int port, out ServerInfo c)
        {
            bool res = services.TryRemove(port, out c);
            if (res)
            {
                try
                {
                    c.Socket.SafeClose();
                    GC.Collect();
                    GC.SuppressFinalize(c);
                }
                catch (Exception)
                {
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
                    item.Socket.SafeClose();
                    GC.Collect();
                    GC.SuppressFinalize(item);
                }
                catch (Exception)
                {
                }
            }
            services.Clear();
        }

    }
    public class ServerInfo
    {
        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
    }
}