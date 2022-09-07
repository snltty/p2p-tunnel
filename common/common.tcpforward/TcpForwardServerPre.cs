using common.libs;
using common.libs.extends;
using common.server.servers.iocp;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace common.tcpforward
{
    public class TcpForwardServerPre : ITcpForwardServer
    {
        BufferManager bufferManager;
        const int opsToPreAlloc = 1;
        SocketAsyncEventArgsPool readWritePool;
        Semaphore maxNumberAcceptedClients;

        public SimpleSubPushHandler<TcpForwardInfo> OnRequest { get; } = new SimpleSubPushHandler<TcpForwardInfo>();
        public SimpleSubPushHandler<ListeningChangeInfo> OnListeningChange { get; } = new SimpleSubPushHandler<ListeningChangeInfo>();
        private readonly ServersManager serversManager = new ServersManager();
        private readonly ClientsManager clientsManager = new ClientsManager();

        private NumberSpace requestIdNs = new NumberSpace(0);

        public TcpForwardServerPre()
        {
        }

        public void Init(int numConnections, int receiveBufferSize)
        {
            bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc, receiveBufferSize);

            readWritePool = new SocketAsyncEventArgsPool(numConnections);
            maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);

            bufferManager.InitBuffer();

            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < numConnections; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += IO_Completed;
                readWriteEventArg.UserToken = new ForwardAsyncUserToken();

                bufferManager.SetBuffer(readWriteEventArg);

                readWritePool.Push(readWriteEventArg);
            }
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
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
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
                maxNumberAcceptedClients.WaitOne();
                if (!token.SourceSocket.AcceptAsync(acceptEventArg))
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
            ForwardAsyncUserToken acceptToken = (e.UserToken as ForwardAsyncUserToken);
            SocketAsyncEventArgs readEventArgs = readWritePool.Pop();
            ForwardAsyncUserToken token = ((ForwardAsyncUserToken)readEventArgs.UserToken);
            try
            {
                token.SourceSocket = e.AcceptSocket;
                token.Request.IsForward = false;
                token.Request.RequestId = requestIdNs.Increment();
                token.Request.AliveType = acceptToken.Request.AliveType;
                token.Request.SourcePort = acceptToken.SourcePort;
                token.SourcePort = acceptToken.SourcePort;
                token.Request.Connection = null;

                clientsManager.TryAdd(token);

                if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
                {
                    ProcessReceive(readEventArgs);
                }
            }
            catch (Exception)
            {
                CloseClientSocket(readEventArgs);
            }
            StartAccept(e);
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    ForwardAsyncUserToken token = (ForwardAsyncUserToken)e.UserToken;
                    Receive(e, e.Buffer, e.Offset, e.BytesTransferred);
                    if (token.SourceSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(token.SourceSocket.Available);
                        while (token.SourceSocket.Available > 0)
                        {
                            int length = token.SourceSocket.Receive(arr);
                            if (length > 0)
                            {
                                Receive(e, arr, 0, length);
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }
                    if (token.SourceSocket.Connected)
                    {
                        if (!token.SourceSocket.ReceiveAsync(e))
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
                if (!token.SourceSocket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void Receive(SocketAsyncEventArgs e, byte[] data, int offset, int length)
        {
            ForwardAsyncUserToken token = (ForwardAsyncUserToken)e.UserToken;
            token.Request.Buffer = data.AsMemory(offset, length);
            OnRequest.Push(token.Request);
            token.Request.IsForward = true;
            token.Request.Buffer = Helper.EmptyArray;
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            ForwardAsyncUserToken token = e.UserToken as ForwardAsyncUserToken;

            clientsManager.TryRemove(token.Request.RequestId, out _);

            readWritePool.Push(e);
            maxNumberAcceptedClients.Release();

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
        public TcpForwardInfo Request { get; set; } = new TcpForwardInfo { IsForward = false };
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
                    c.SourceSocket.SafeClose();
                    GC.Collect();
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