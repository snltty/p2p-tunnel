using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace common.server.servers.websocket
{
    public class WebSocketClient : IDisposable
    {
        private int bufferSize = 4 * 1024;
        private SocketAsyncEventArgs readEventArgs;
        private bool connected = false;
        private bool connecSuccess = false;

        
        // <summary>
        /// 连接中，false表示失败，会关闭连接
        /// </summary>
        public Func<WebsocketHeaderInfo, bool> OnConnecting = (header) => { return true; };
        /// <summary>
        /// 连接失败
        /// </summary>
        public Action<string> OnConnectFail { get; set; } = (msg) => { };
        // <summary>
        /// 已断开连接,未收到关闭帧
        /// </summary>
        public Action OnDisConnectd = () => { };

        // <summary>
        /// 已连接
        /// </summary>
        public Action<WebsocketHeaderInfo> OnOpen = (header) => { };
        // <summary>
        /// 已断开连接,收到关闭帧
        /// </summary>
        public Action OnClose = () => { };

        /// <summary>
        /// 文本数据
        /// </summary>
        public Action<WebSocketFrameInfo, string> OnMessage = (frame, messgae) => { };
        /// <summary>
        /// 二进制数据
        /// </summary>
        public Action<WebSocketFrameInfo, Memory<byte>> OnBinary = (frame, data) => { };

        /// <summary>
        /// 控制帧
        /// </summary>
        public Action<WebSocketFrameInfo> OnControll = (frame) => { };
        /// <summary>
        /// 非控制帧
        /// </summary>
        public Action<WebSocketFrameInfo> OnUnControll = (frame) => { };

        public WebSocketClient()
        {
            handles = new Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncServerUserToken>> {
                //直接添加数据
                { WebSocketFrameInfo.EnumOpcode.Data,HandleAppendData},
                //记录opcode并添加
                { WebSocketFrameInfo.EnumOpcode.Text,HandleData},
                { WebSocketFrameInfo.EnumOpcode.Binary,HandleData},

                { WebSocketFrameInfo.EnumOpcode.Close,HandleClose},

                { WebSocketFrameInfo.EnumOpcode.Ping,HandlePing},
                { WebSocketFrameInfo.EnumOpcode.Pong,HandlePong},
            };
        }

        public void Connect(IPAddress ip, int port)
        {
            Connect(new IPEndPoint(ip, port));
        }
        public void Connect(IPEndPoint ep)
        {
            if (connected)
            {
                throw new Exception("connected");
            }

            var socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.KeepAlive();

            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = socket,
            };
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            connectEventArgs.RemoteEndPoint = ep;
            connectEventArgs.Completed += Target_IO_Completed;
            if (!socket.ConnectAsync(connectEventArgs))
            {
                TargetProcessConnect(connectEventArgs);
            }
        }

        private void Target_IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    TargetProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    TargetProcessReceive();
                    break;
                default:
                    break;
            }
        }
        private void TargetProcessConnect(SocketAsyncEventArgs connectEventArgs)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)connectEventArgs.UserToken;
            try
            {
                if (connectEventArgs.SocketError == SocketError.Success)
                {

                    token.SecWebSocketKey = WebSocketParser.BuildSecWebSocketKey();
                    byte[] connectData = WebSocketParser.BuildConnectData(new WebsocketHeaderInfo
                    {
                        SecWebSocketKey = token.SecWebSocketKey,
                    });
                    token.TargetSocket.Send(connectData);

                    connected = true;
                    BindTargetReceive(token);
                }
                else
                {
                    OnConnectFail(connectEventArgs.SocketError.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                OnConnectFail(ex.Message);
            }
        }
        private void BindTargetReceive(AsyncServerUserToken connectToken)
        {
            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = connectToken.TargetSocket,
                SecWebSocketKey = connectToken.SecWebSocketKey,
            };
            try
            {
                readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None,
                };
                token.PoolBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                readEventArgs.SetBuffer(token.PoolBuffer, 0, bufferSize);
                readEventArgs.Completed += Target_IO_Completed;
                if (!token.TargetSocket.ReceiveAsync(readEventArgs))
                {
                    TargetProcessReceive();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
            }
        }
        private void TargetProcessReceive()
        {
            try
            {
                AsyncServerUserToken token = (AsyncServerUserToken)readEventArgs.UserToken;
                if (readEventArgs.BytesTransferred > 0 && readEventArgs.SocketError == SocketError.Success)
                {
                    int offset = readEventArgs.Offset;
                    int length = readEventArgs.BytesTransferred;

                    ReadFrame(token, readEventArgs.Buffer.AsMemory(offset, length));
                    if (token.TargetSocket.Available > 0)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(token.TargetSocket.Available);
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(arr);
                            if (length > 0)
                            {
                                ReadFrame(token, arr.AsMemory(0, length));
                            }
                        }
                        ArrayPool<byte>.Shared.Return(arr);
                    }

                    if (!token.TargetSocket.Connected)
                    {
                        CloseClientSocket();
                        return;
                    }
                    if (!token.TargetSocket.ReceiveAsync(readEventArgs))
                    {
                        TargetProcessReceive();
                    }
                }
                else
                {
                    CloseClientSocket();
                }
            }
            catch (Exception ex)
            {
                CloseClientSocket();
                Logger.Instance.DebugError(ex);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if (connected)
            {
                AsyncServerUserToken token = readEventArgs.UserToken as AsyncServerUserToken;
                token.Clear();
                readEventArgs.Dispose();
                OnDisConnectd();
            }
            connected = false;
            connecSuccess = false;
        }
        private void CloseClientSocket()
        {
            CloseClientSocket(readEventArgs);
        }
        public void Close()
        {
            CloseClientSocket();
        }


        private readonly Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncServerUserToken>> handles;
        private void ReadFrame(AsyncServerUserToken token, Memory<byte> data)
        {
            if (connecSuccess)
            {
                if (token.FrameBuffer.Size == 0 && data.Length > 6)
                {
                    if (WebSocketFrameInfo.TryParse(data, out token.FrameInfo))
                    {
                        ExecuteHandle(token);
                        if (token.FrameInfo.TotalLength == data.Length)
                        {
                            return;
                        }
                        token.FrameBuffer.AddRange(data.Slice(token.FrameInfo.TotalLength));
                    }
                    else
                    {
                        token.FrameBuffer.AddRange(data);
                    }
                }
                else
                {
                    token.FrameBuffer.AddRange(data);
                }

                do
                {
                    if (!WebSocketFrameInfo.TryParse(token.FrameBuffer.Data.Slice(token.FrameIndex), out token.FrameInfo))
                    {
                        break;
                    }
                    if (token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
                    {
                        ExecuteHandle(token);
                        token.FrameBuffer.RemoveRange(0, token.FrameInfo.TotalLength);
                        token.FrameIndex = 0;
                    }
                    else
                    {
                        token.FrameBuffer.RemoveRange(token.FrameIndex, token.FrameInfo.TotalLength - token.FrameInfo.PayloadData.Length);
                        token.FrameIndex += token.FrameInfo.PayloadData.Length;
                    }
                } while (token.FrameBuffer.Size > 6);
            }
            else
            {
                HandleConnect(token, data);
            }
        }
        private void ExecuteHandle(AsyncServerUserToken token)
        {
            if (handles.TryGetValue(token.FrameInfo.Opcode, out Action<AsyncServerUserToken> action))
            {
                action(token);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll3 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll7)
            {
                OnUnControll(token.FrameInfo);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll11 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll15)
            {
                OnControll(token.FrameInfo);
            }
            else
            {
                SendFrameClose(WebSocketFrameInfo.EnumCloseStatus.Unexpected);
                CloseClientSocket();
                return;
            }
        }
        private void HandleData(AsyncServerUserToken token)
        {
            token.Opcode = token.FrameInfo.Opcode;
            HandleAppendData(token);
        }
        private void HandleAppendData(AsyncServerUserToken token)
        {
            if (token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
            {
                if (token.Opcode == WebSocketFrameInfo.EnumOpcode.Text)
                {
                    string str = token.FrameInfo.PayloadData.GetString();
                    OnMessage(token.FrameInfo, str);
                }
                else
                {
                    OnBinary(token.FrameInfo, token.FrameInfo.PayloadData);
                }
            }
        }
        private void HandleClose(AsyncServerUserToken token)
        {
            SendFrameClose(WebSocketFrameInfo.EnumCloseStatus.Normal);
            CloseClientSocket();
            OnClose();
        }
        private void HandlePing(AsyncServerUserToken token)
        {
            SendFramePong();
        }
        private void HandlePong(AsyncServerUserToken token) { }
        private void HandleConnect(AsyncServerUserToken token, Memory<byte> data)
        {
            WebsocketHeaderInfo header = WebsocketHeaderInfo.Parse(data);
            if (!WebSocketParser.VerifySecWebSocketAccept(token.SecWebSocketKey, header.SecWebSocketAccept))
            {
                OnConnectFail("Sec-WebSocket-Accept Invalid");
                CloseClientSocket();
                return;
            }
            if (header.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                OnConnectFail($"{(int)header.StatusCode},{header.StatusCode}");
                CloseClientSocket();
                return;
            }
            if (!OnConnecting(header))
            {
                CloseClientSocket();
                return;
            }

            connecSuccess = true;
            OnOpen(header);
        }

        public int SendRaw(byte[] buffer)
        {
            return (readEventArgs.UserToken as AsyncServerUserToken).TargetSocket.Send(buffer);
        }
        public int SendFrame(WebSocketFrameRemarkInfo remark)
        {
            remark.Mask = WebSocketFrameInfo.EnumMask.Mask;
            remark.MaskData = WebSocketParser.BuildMaskKey();
            var frame = WebSocketParser.BuildFrameData(remark);
            return SendRaw(frame);
        }
        public int SendFrameText(string txt)
        {
            return SendFrameText(txt.ToBytes());
        }
        public int SendFrameText(byte[] buffer)
        {
            return SendFrame(new WebSocketFrameRemarkInfo
            {
                Opcode = WebSocketFrameInfo.EnumOpcode.Text,
                Data = buffer
            });
        }
        public int SendFrameBinary(byte[] buffer)
        {
            return SendFrame(new WebSocketFrameRemarkInfo
            {
                Opcode = WebSocketFrameInfo.EnumOpcode.Binary,
                Data = buffer
            });
        }
        public int SendFramePoing()
        {
            return SendRaw(WebSocketParser.BuildPingData());
        }
        public int SendFramePong()
        {
            return SendRaw(WebSocketParser.BuildPongData());
        }
        public int SendFrameClose(WebSocketFrameInfo.EnumCloseStatus status)
        {
            return SendFrame(new WebSocketFrameRemarkInfo
            {
                Opcode = WebSocketFrameInfo.EnumOpcode.Close,
                Data = ((short)status).ToBytes()
            });
        }

        public void Dispose()
        {
            CloseClientSocket();
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }

    public class AsyncServerUserToken
    {
        public Socket TargetSocket { get; set; }

        /// <summary>
        /// 当前帧数据
        /// </summary>
        public WebSocketFrameInfo FrameInfo = null;
        /// <summary>
        /// 当前帧的数据下标
        /// </summary>
        public int FrameIndex { get; set; } = 0;
        /// <summary>
        /// 数据帧缓存
        /// </summary>
        public ReceiveDataBuffer FrameBuffer { get; } = new ReceiveDataBuffer();
        /// <summary>
        /// 当前帧的数据类型
        /// </summary>
        public WebSocketFrameInfo.EnumOpcode Opcode { get; set; }

        public Memory<byte> SecWebSocketKey { get; set; }

        public byte[] PoolBuffer { get; set; }


        public void Clear()
        {
            TargetSocket?.SafeClose();
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
                PoolBuffer = null;
            }
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}
