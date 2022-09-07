using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace common.socks5
{
    public interface ISocks5ClientListener
    {
        IPEndPoint DistEndpoint { get; }
        byte Version { get; }
        Func<Socks5Info, bool> OnData { get; set; }
        Action<Socks5Info> OnClose { get; set; }
        void Start(int port, int bufferSize);
        void Response(Socks5Info info);
        void Close(ulong id);
        void Stop();
    }

    public class Socks5ClientListener : ISocks5ClientListener
    {
        private Socket socket;
        private UdpClient udpClient;
        private int bufferSize = 8 * 1024;
        public IPEndPoint DistEndpoint { get; private set; }
        public byte Version { get; private set; } = 5;


        private readonly ConcurrentDictionary<ulong, AsyncUserToken> connections = new();
        private readonly NumberSpace numberSpace = new NumberSpace(0);

        public Func<Socks5Info, bool> OnData { get; set; }
        public Action<Socks5Info> OnClose { get; set; }

        public Socks5ClientListener()
        {
        }

        public void Start(int port, int bufferSize)
        {
            this.bufferSize = bufferSize;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            DistEndpoint = new IPEndPoint(IPAddress.Loopback, port);

            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = new AsyncUserToken
                {
                    Socket = socket,
                },
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;

            StartAccept(acceptEventArg);

            udpClient = new UdpClient(localEndPoint);
            IAsyncResult result = udpClient.BeginReceive(ProcessReceiveUdp, null);
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
                    Logger.Instance.DebugError(e.LastOperation.ToString());
                    break;
            }
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            AsyncUserToken token = ((AsyncUserToken)acceptEventArg.UserToken);
            try
            {
                if (!token.Socket.AcceptAsync(acceptEventArg))
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {

            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            BindReceive(e.AcceptSocket);
            StartAccept(e);
        }
        private void BindReceive(Socket socket)
        {
            ulong id = numberSpace.Increment();
            AsyncUserToken token = new AsyncUserToken
            {
                Socket = socket,
                DataWrap = new Socks5Info { Id = id }
            };
            connections.TryAdd(token.DataWrap.Id, token);
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            token.PoolBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            readEventArgs.SetBuffer(token.PoolBuffer, 0, bufferSize);
            readEventArgs.Completed += IO_Completed;
            if (!socket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {

            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    Memory<byte> buffer = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    token.DataWrap.Data = buffer;

                    ExecuteHandle(token.DataWrap);
                    token.DataWrap.Data = Helper.EmptyArray;

                    if (token.Socket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(token.Socket.Available);
                        while (token.Socket.Available > 0)
                        {
                            int length = token.Socket.Receive(arr);
                            if (length > 0)
                            {
                                token.DataWrap.Data = arr.AsMemory(0, length);
                                ExecuteHandle(token.DataWrap);
                                token.DataWrap.Data = Helper.EmptyArray;
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (!token.Socket.Connected)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (!token.Socket.ReceiveAsync(e))
                    {
                        ProcessReceive(e);
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
        Socks5Info udpInfo = new Socks5Info { Id = 0, Socks5Step = Socks5EnumStep.ForwardUdp };
        private void ProcessReceiveUdp(IAsyncResult result)
        {
            IPEndPoint rep = null;
            try
            {
                udpInfo.Data = udpClient.EndReceive(result, ref rep);
                udpInfo.SourceEP = rep;

                ExecuteHandle(udpInfo);
                udpInfo.Data = Helper.EmptyArray;

                result = udpClient.BeginReceive(ProcessReceiveUdp, null);
            }
            catch (Exception)
            {
            }
        }
        private void ExecuteHandle(Socks5Info info)
        {
            if (OnData != null)
            {
                if (!OnData(info))
                {
                    CloseClientSocket(info.Id);
                }
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (!token.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        public void Response(Socks5Info info)
        {
            if (connections.TryGetValue(info.Id, out AsyncUserToken token))
            {
                if (info.Data.Length == 0)
                {
                    CloseClientSocket(info.Id);
                }
                else
                {
                    Socks5EnumStep step = token.DataWrap.Socks5Step;
                    token.DataWrap.Socks5Step = info.Socks5Step;
                    if (step == Socks5EnumStep.ForwardUdp)
                    {
                        udpClient.Send(info.Data.Span, info.SourceEP);
                    }
                    else
                    {
                        token.Socket.Send(info.Data.Span);
                    }

                }
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.Disposabled == false)
            {
                e.Dispose();
                if (connections.TryRemove(token.DataWrap.Id, out _))
                {
                    if (OnClose != null && token.Disposabled == false)
                    {
                        OnClose(token.DataWrap);
                    }
                    token.Clear();
                }
            }
        }
        private void CloseClientSocket(ulong id)
        {
            if (connections.TryRemove(id, out AsyncUserToken token))
            {
                token.Clear();
            }
        }

        public void Close(ulong id)
        {
            CloseClientSocket(id);
        }
        public void Stop()
        {
            socket?.SafeClose();
            udpClient?.Dispose();
            foreach (var item in connections.Values)
            {
                item.Clear();
            }
            connections.Clear();
        }
    }


    public class AsyncUserToken
    {
        public Socket Socket { get; set; }
        public byte[] PoolBuffer { get; set; }
        public Socks5Info DataWrap { get; set; }

        public bool Disposabled { get; private set; } = false;

        public void Clear()
        {
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }
            Socket?.SafeClose();
            Disposabled = true;
        }
    }

}
