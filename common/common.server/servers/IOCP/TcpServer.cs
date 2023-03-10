using common.libs;
using common.libs.extends;
using common.server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace common.server.servers.iocp
{
    public class TcpServer : ITcpServer
    {
        private int bufferSize = 8 * 1024;
        private int port = 0;
        private Socket socket;
        private CancellationTokenSource cancellationTokenSource;
        bool isReceive = true;
        public Func<IConnection, Task> OnPacket { get; set; } = async (connection) => { await Task.CompletedTask; };
        public Action<IPEndPoint, Memory<byte>> OnMessage { get; set; }
        public Action<IConnection> OnDisconnect { get; set; } = (IConnection connection) => { };
        public Action<IConnection> OnConnected { get; set; } = (connection) => { };
        public Action<Socket> OnConnected1 { get; set; } = (socket) => { };
       

        public TcpServer() { }
        public void SetBufferSize(int bufferSize = 8 * 1024)
        {
            this.bufferSize = bufferSize;
        }
        public void Start1(int port)
        {
            isReceive = false;
            Start(port);
        }
        public void Start(int port)
        {
            if (socket == null)
            {
                this.port = port;
                cancellationTokenSource = new CancellationTokenSource();
                socket = BindAccept(port);
            }
        }

        private Socket BindAccept(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.ReuseBind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = new AsyncUserToken
                {
                    Socket = socket,
                    Port = port
                },
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            return socket;

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
                token.Clear();
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
                default:
                    break;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (isReceive)
            {
                if (e.AcceptSocket != null)
                {
                    BindReceive(e.AcceptSocket, bufferSize);
                }
                StartAccept(e);
            }
            else
            {
                if (OnConnected1 != null)
                {
                    OnConnected1(e.AcceptSocket);
                }
            }
        }

        public IConnection BindReceive(Socket socket, int bufferSize = 8 * 1024)
        {
            try
            {
                if (socket == null)
                {
                    return null;
                }


                this.bufferSize = bufferSize;
                AsyncUserToken userToken = new AsyncUserToken
                {
                    Socket = socket,
                    Connection = CreateConnection(socket),
                    Port = port
                };

                OnConnected(userToken.Connection);
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                //socket.ReceiveBufferSize = bufferSize;
                //socket.SendBufferSize = bufferSize;
                userToken.PoolBuffer = new byte[bufferSize];
                readEventArgs.SetBuffer(userToken.PoolBuffer, 0, bufferSize);
                readEventArgs.Completed += IO_Completed;
                if (socket.ReceiveAsync(readEventArgs) == false)
                {
                    ProcessReceive(readEventArgs);
                }
                return userToken.Connection;
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
            }
            return null;
        }
        private async void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    await ReadPacket(token, e.Buffer, offset, length);

                    if (token.Socket.Available > 0)
                    {
                        while (token.Socket.Available > 0)
                        {
                            length = token.Socket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await ReadPacket(token, e.Buffer, 0, length);
                            }
                            else
                            {
                                token.Connection.SocketError = SocketError.SocketError;
                                CloseClientSocket(e);
                                return;
                            }
                        }
                    }

                    if (token.Socket.Connected == false || token.Port != port)
                    {
                        token.Connection.SocketError = SocketError.SocketError;
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
                    token.Connection.SocketError = e.SocketError;
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                CloseClientSocket(e);
            }
        }
        private async Task ReadPacket(AsyncUserToken token, byte[] data, int offset, int length)
        {
            if (token.Port != port) return;

            //是一个完整的包
            if (token.DataBuffer.Size == 0 && length > 4)
            {
                Memory<byte> memory = data.AsMemory(offset, length);
                int packageLen = memory.Span.ToInt32();
                if (packageLen == length - 4)
                {
                    token.Connection.ReceiveData = data.AsMemory(offset, packageLen + 4);
                    await OnPacket(token.Connection);
                    return;
                }
            }

            //不是完整包
            token.DataBuffer.AddRange(data, offset, length);
            do
            {
                int packageLen = token.DataBuffer.Data.Span.ToInt32();
                if (packageLen > token.DataBuffer.Size - 4)
                {
                    break;
                }
                token.Connection.ReceiveData = token.DataBuffer.Data.Slice(0, packageLen + 4);
                await OnPacket(token.Connection);

                token.DataBuffer.RemoveRange(0, packageLen + 4);
            } while (token.DataBuffer.Size > 4);
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.Socket != null)
            {
                token.Clear();
                e.Dispose();
            }
            OnDisconnect?.Invoke(token.Connection);
        }

        public IConnection CreateConnection(Socket socket)
        {
            return new TcpConnection(socket)
            {
                ReceiveRequestWrap = new MessageRequestWrap(),
                ReceiveResponseWrap = new MessageResponseWrap()
            };
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            socket?.SafeClose();
            socket = null;
            port = 0;
        }

        public void Disponse()
        {
            Stop();
            OnPacket = null;
            OnDisconnect = null;
            OnConnected = null;
            OnConnected1 = null;
        }

        public async Task InputData(IConnection connection)
        {
            if (OnPacket != null)
            {
                await OnPacket(connection);
            }
        }
    }

   
    public class AsyncUserToken
    {
        public IConnection Connection { get; set; }
        public Socket Socket { get; set; }
        public ReceiveDataBuffer DataBuffer { get; set; } = new ReceiveDataBuffer();
        public int Port { get; set; }
        public byte[] PoolBuffer { get; set; }
        public void Clear()
        {
            Socket?.SafeClose();
            Socket = null;

            PoolBuffer = Helper.EmptyArray;

            DataBuffer.Clear(true);

            GC.Collect();
            // GC.SuppressFinalize(this);
        }
    }
}
