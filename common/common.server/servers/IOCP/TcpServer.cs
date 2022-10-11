using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
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
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; private set; } = new SimpleSubPushHandler<IConnection>();
        public Action<IConnection> OnConnected { get; set; } = (connection) => { };
        public Action<Socket> OnConnected1 { get; set; } = (socket) => { };

        public TcpServer() { }

        public void SetBufferSize(int bufferSize = 8 * 1024)
        {
            this.bufferSize = bufferSize;
        }

        public void Start1(int port, IPAddress ip )
        {
            isReceive = false;
            Start(port, ip);
        }
        public void Start(int port, IPAddress ip)
        {
            if (socket == null)
            {
                this.port = port;
                cancellationTokenSource = new CancellationTokenSource();
                socket = BindAccept(port, ip ?? IPAddress.Any);
            }
        }

        public Socket BindAccept(int port, IPAddress ip)
        {
            IPEndPoint localEndPoint = new IPEndPoint(ip, port);

            var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
                if (!token.Socket.AcceptAsync(acceptEventArg))
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
                    // Logger.Instance.DebugError(e.LastOperation.ToString());
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
                if(OnConnected1 != null)
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
                    Port = this.port
                };

                if (OnConnected == null)
                {
                    return null;
                }
                OnConnected(userToken.Connection);
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                userToken.PoolBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                readEventArgs.SetBuffer(userToken.PoolBuffer, 0, bufferSize);
                readEventArgs.Completed += IO_Completed;
                if (!socket.ReceiveAsync(readEventArgs))
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
                        var arr = ArrayPool<byte>.Shared.Rent(bufferSize);
                        while (token.Socket.Available > 0)
                        {
                            length = token.Socket.Receive(arr);
                            if (length > 0)
                            {
                                await ReadPacket(token, arr, 0, length);
                            }
                            else
                            {
                                token.Connection.SocketError = SocketError.SocketError;
                                CloseClientSocket(e);
                                ArrayPool<byte>.Shared.Return(arr);
                                return;
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (token.Socket.Connected == false || token.Port != port)
                    {
                        token.Connection.SocketError = SocketError.SocketError;
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

            if (token.DataBuffer.Size == 0 && length > 4)
            {
                Memory<byte> memory = data.AsMemory(offset, length);
                int packageLen = memory.Span.ToInt32();
                if (packageLen == length - 4)
                {
                    token.Connection.ReceiveData = data.AsMemory(offset + 4, packageLen);
                    if (OnPacket != null)
                    {
                        await OnPacket(token.Connection);
                    }
                    return;
                }
            }

            token.DataBuffer.AddRange(data, offset, length);
            do
            {
                int packageLen = token.DataBuffer.Data.Span.ToInt32();
                if (packageLen > token.DataBuffer.Size - 4)
                {
                    break;
                }
                token.Connection.ReceiveData = token.DataBuffer.Data.Slice(4, packageLen);
                if (OnPacket != null)
                {
                    await OnPacket(token.Connection);
                }

                token.DataBuffer.RemoveRange(0, packageLen + 4);
            } while (token.DataBuffer.Size > 4);
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
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.Socket != null)
            {
                token.Clear();
                e.Dispose();
                if (OnDisconnect != null)
                {
                    OnDisconnect.Push(token.Connection);
                }
            }
        }

        public IConnection CreateConnection(Socket socket)
        {
            return new TcpConnection(socket);
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
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }

            Socket?.SafeClose();
            Socket = null;

            DataBuffer.Clear(true);
        }
    }
}
