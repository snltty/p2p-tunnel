using common.libs;
using common.libs.extends;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocks5ClientListener
    {
        /// <summary>
        /// 
        /// </summary>
        IPEndPoint DistEndpoint { get; }
        /// <summary>
        /// 
        /// </summary>
        byte Version { get; }
        /// <summary>
        /// 
        /// </summary>
        Func<Socks5Info, bool> OnData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        Action<Socks5Info> OnClose { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        void Start(int port, int bufferSize);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        void Response(Socks5Info info);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        void Close(ulong id);
        /// <summary>
        /// 
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// 
    /// </summary>
    public class Socks5ClientListener : ISocks5ClientListener
    {
        private Socket socket;
        private UdpClient udpClient;
        private int bufferSize = 8 * 1024;
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint DistEndpoint { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public byte Version { get; private set; } = 5;


        private readonly ConcurrentDictionary<ulong, AsyncUserToken> connections = new();
        private readonly NumberSpaceUInt32 numberSpace = new NumberSpaceUInt32(0);
        /// <summary>
        /// 
        /// </summary>
        public Func<Socks5Info, bool> OnData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Action<Socks5Info> OnClose { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5ClientListener()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
        public void Start(int port, int bufferSize)
        {
            this.bufferSize = bufferSize;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            DistEndpoint = new IPEndPoint(IPAddress.Loopback, port);

            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
                if (token.Socket.AcceptAsync(acceptEventArg) == false)
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
            if (socket == null) return;

            uint id = numberSpace.Increment();
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
            token.PoolBuffer = new byte[bufferSize];
            readEventArgs.SetBuffer(token.PoolBuffer, 0, bufferSize);
            readEventArgs.Completed += IO_Completed;
            if (socket.ReceiveAsync(readEventArgs) == false)
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
                    token.DataWrap.Data = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    ExecuteHandle(token.DataWrap);
                    token.DataWrap.Data = Helper.EmptyArray;

                    if (token.Socket.Available > 0)
                    {
                        while (token.Socket.Available > 0)
                        {
                            int length = token.Socket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                token.DataWrap.Data = e.Buffer.AsMemory(0, length);
                                ExecuteHandle(token.DataWrap);
                                token.DataWrap.Data = Helper.EmptyArray;
                            }
                        }
                    }

                    if (token.Socket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (token.Socket.ReceiveAsync(e) == false)
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
                if (OnData(info) == false)
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
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
                    token.DataWrap.Socks5Step = info.Socks5Step;
                    if (info.Socks5Step == Socks5EnumStep.ForwardUdp)
                    {
                        udpClient.Send(info.Data.Span, info.SourceEP);
                    }
                    else
                    {
                        token.Socket.Send(info.Data.Span, SocketFlags.None);
                    }

                }
            }
            else if (info.SourceEP != null)
            {
                udpClient.Send(info.Data.Span, info.SourceEP);
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void Close(ulong id)
        {
            CloseClientSocket(id);
        }
        /// <summary>
        /// 
        /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    public class AsyncUserToken
    {
        /// <summary>
        /// 
        /// </summary>
        public Socket Socket { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] PoolBuffer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5Info DataWrap { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Disposabled { get; private set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            Socket?.SafeClose();
            Socket = null;
            PoolBuffer = Helper.EmptyArray;

            Disposabled = true;
        }
    }

}
