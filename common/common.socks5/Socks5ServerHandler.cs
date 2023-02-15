using common.libs;
using common.libs.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public class Socks5ServerHandler : ISocks5ServerHandler
    {
        private ConcurrentDictionary<ConnectionKey, AsyncServerUserToken> connections = new(new ConnectionKeyComparer());
        private ConcurrentDictionary<ConnectionKeyUdp, UdpToken> udpConnections = new(new ConnectionKeyUdpComparer());
        private readonly Dictionary<Socks5EnumStep, Action<Socks5Info>> handles = new Dictionary<Socks5EnumStep, Action<Socks5Info>>();
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        private readonly ISocks5MessengerSender socks5MessengerSender;

        /// <summary>
        /// 
        /// </summary>
        protected Config config { get; }
        private readonly WheelTimer<object> wheelTimer;
        private readonly ISocks5Validator socks5Validator;
        private readonly ISocks5AuthValidator socks5AuthValidator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socks5MessengerSender"></param>
        /// <param name="config"></param>
        /// <param name="wheelTimer"></param>
        /// <param name="socks5Validator"></param>
        public Socks5ServerHandler(ISocks5MessengerSender socks5MessengerSender, Config config, WheelTimer<object> wheelTimer, ISocks5Validator socks5Validator, ISocks5AuthValidator socks5AuthValidator)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.config = config;

            this.wheelTimer = wheelTimer;
            this.socks5Validator = socks5Validator;
            this.socks5AuthValidator = socks5AuthValidator;
            TimeoutUdp();

            handles = new Dictionary<Socks5EnumStep, Action<Socks5Info>> {
                {Socks5EnumStep.Request, HandleRequest},
                {Socks5EnumStep.Auth, HandleAuth},
                {Socks5EnumStep.Command, HandleCommand},
                {Socks5EnumStep.Forward, HndleForward},
                {Socks5EnumStep.ForwardUdp, HndleForwardUdp},
            };

        }

        public void InputData(Socks5Info data)
        {
            if (data.Data.Length == 0)
            {
                data.Socks5Step = Socks5EnumStep.Forward;
            }
            if (handles.TryGetValue(data.Socks5Step, out Action<Socks5Info> action))
            {
                action(data);
            }
        }

        private void HandleRequest(Socks5Info data)
        {
            data.AuthType = socks5AuthValidator.GetAuthType(Socks5Parser.GetAuthMethods(data.Data.Span));
            data.Response[0] = (byte)data.AuthType;
            data.Data = data.Response;
            _ = Receive(data);
        }
        private void HandleAuth(Socks5Info data)
        {
            data.Response[0] = (byte)socks5AuthValidator.Validate(data.Data, data.AuthType);
            data.Data = data.Response;
            _ = Receive(data);
        }
        private void HndleForward(Socks5Info data)
        {
            ConnectionKey key = new ConnectionKey(data.ClientId, data.Id);
            if (connections.TryGetValue(key, out AsyncServerUserToken token))
            {
                if (data.Data.Length > 0 && token.TargetSocket.Connected)
                {
                    try
                    {
                        token.TargetSocket.Send(data.Data.Span, SocketFlags.None);
                    }
                    catch (Exception)
                    {
                        CloseClientSocket(token);
                    }
                }
                else
                {
                    CloseClientSocket(token);
                }
            }
        }
        private void HndleForwardUdp(Socks5Info data)
        {
            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(data.Data);
            if (remoteEndPoint.Port == 0) return;
            Memory<byte> sendData = Socks5Parser.GetUdpData(data.Data);

            ConnectionKeyUdp key = new ConnectionKeyUdp(data.ClientId, data.SourceEP);
            try
            {

                if (udpConnections.TryGetValue(key, out UdpToken token) == false)
                {
                    data.TargetEP = remoteEndPoint;
                    Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    socket.WindowsUdpBug();
                    token = new UdpToken { Data = data, TargetSocket = socket, Key = key };
                    token.PoolBuffer = new byte[65535];
                    udpConnections.AddOrUpdate(key, token, (a, b) => token);

                    token.TargetSocket.SendTo(sendData.Span, SocketFlags.None, remoteEndPoint);
                    token.Data.Data = Helper.EmptyArray;
                    IAsyncResult result = socket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
                }
                else
                {
                    token.Update();
                    token.TargetSocket.SendTo(sendData.Span, SocketFlags.None, remoteEndPoint);
                    token.Data.Data = Helper.EmptyArray;
                }
            }
            catch (Exception ex)
            {
                if (udpConnections.TryRemove(key, out UdpToken _token))
                {
                    _token.Clear();
                }
                Logger.Instance.DebugError($"socks5 forward udp -> sendto {remoteEndPoint} : {sendData.Length}  " + ex);
            }
        }
        private void TimeoutUdp()
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                State = null,
                Callback = (timeout) =>
                {
                    long time = DateTimeHelper.GetTimeStamp();

                    var tokens = udpConnections.Where(c => time - c.Value.LastTime > (60 * 1000));
                    foreach (var item in tokens)
                    {
                        if (udpConnections.TryRemove(item.Key, out _))
                        {
                            item.Value.Clear();
                        }
                    }
                }
            }, 1000, true);
        }
        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            UdpToken token = result.AsyncState as UdpToken;
            try
            {
                int length = token.TargetSocket.EndReceiveFrom(result, ref token.TempRemoteEP);
                if (length > 0)
                {
                    token.Data.Data = token.PoolBuffer.AsMemory(0, length);

                    token.Update();
                    await Receive(token.Data);
                    token.Data.Data = Helper.EmptyArray;
                }
                result = token.TargetSocket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
            }
            catch (Exception ex)
            {
                if (udpConnections.TryRemove(token.Key, out _))
                {
                    token.Clear();
                }
                Logger.Instance.DebugError($"socks5 forward udp -> receive" + ex);
            }
        }


        private void HandleCommand(Socks5Info data)
        {
            try
            {
                if (socks5Validator.Validate(data) == false)
                {
                    _ = ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow);
                    return;
                }

                Socks5EnumRequestCommand command = (Socks5EnumRequestCommand)data.Data.Span[1];
                IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(data.Data);
                if (remoteEndPoint.Port == 0)
                {
                    _ = ConnectReponse(data, Socks5EnumResponseCommand.NetworkError);
                    return;
                }

                if (command == Socks5EnumRequestCommand.Connect)
                {
                    Connect(data, remoteEndPoint);
                }
                else if (command == Socks5EnumRequestCommand.UdpAssociate)
                {
                    _ = ConnectReponse(data, Socks5EnumResponseCommand.ConnecSuccess);
                }
                else if (command == Socks5EnumRequestCommand.Bind)
                {
                    _ = ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow);
                }
                else
                {
                    _ = ConnectReponse(data, Socks5EnumResponseCommand.CommandNotAllow);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                _ = ConnectReponse(data, Socks5EnumResponseCommand.ConnectFail);
                return;
            }


        }
        private void Connect(Socks5Info data, IPEndPoint remoteEndPoint)
        {
            //maxNumberAcceptedClients.WaitOne();
            Socket socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, true);
            socket.SendTimeout = 5000;

            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = socket,
                Data = data,
                Key = new ConnectionKey(data.ClientId, data.Id)
            };
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None
            };
            connectEventArgs.RemoteEndPoint = remoteEndPoint;
            connectEventArgs.Completed += Target_IO_Completed;
            if (socket.ConnectAsync(connectEventArgs) == false)
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
                    TargetProcessReceive(e);
                    break;
                default:
                    break;
            }
        }
        private async void TargetProcessConnect(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
            Socks5EnumResponseCommand command = Socks5EnumResponseCommand.ServerError;
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    BindTargetReceive(token);
                    await ConnectReponse(token.Data, Socks5EnumResponseCommand.ConnecSuccess);
                    token.Data.Socks5Step = Socks5EnumStep.Forward;
                    return;
                }
                else
                {
                    if (e.SocketError == SocketError.ConnectionRefused)
                    {
                        command = Socks5EnumResponseCommand.DistReject;
                    }
                    else if (e.SocketError == SocketError.NetworkDown)
                    {
                        command = Socks5EnumResponseCommand.NetworkError;
                    }
                    else if (e.SocketError == SocketError.ConnectionReset)
                    {
                        command = Socks5EnumResponseCommand.DistReject;
                    }
                    else if (e.SocketError == SocketError.AddressFamilyNotSupported || e.SocketError == SocketError.OperationNotSupported)
                    {
                        command = Socks5EnumResponseCommand.AddressNotAllow;
                    }
                    else
                    {
                        command = Socks5EnumResponseCommand.ServerError;
                    }
                    await ConnectReponse(token.Data, command);
                    CloseClientSocket(token);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                command = Socks5EnumResponseCommand.ServerError;
                await ConnectReponse(token.Data, command);
                CloseClientSocket(token);
            }
        }
        private async Task ConnectReponse(Socks5Info data, Socks5EnumResponseCommand command)
        {
            data.Response[0] = (byte)command;
            data.Data = data.Response;
            await Receive(data);
        }

        private void BindTargetReceive(AsyncServerUserToken connectToken)
        {
            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = connectToken.TargetSocket,
                Key = connectToken.Key,
                Data = connectToken.Data
            };
            connections.TryAdd(token.Key, token);
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            token.PoolBuffer = new byte[config.BufferSize];
            readEventArgs.SetBuffer(token.PoolBuffer, 0, config.BufferSize);
            readEventArgs.Completed += Target_IO_Completed;

            if (token.TargetSocket.ReceiveAsync(readEventArgs) == false)
            {
                TargetProcessReceive(readEventArgs);
            }
        }
        private async void TargetProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                AsyncServerUserToken token = (AsyncServerUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    if (token.Data.Socks5Step < Socks5EnumStep.Forward)
                    {
                        await ConnectReponse(token.Data, Socks5EnumResponseCommand.ConnecSuccess);
                        token.Data.Socks5Step = Socks5EnumStep.Forward;
                    }

                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    token.Data.Data = e.Buffer.AsMemory(offset, length);
                    await Receive(token.Data);
                    token.Data.Data = Helper.EmptyArray;

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = await token.TargetSocket.ReceiveAsync(e.Buffer.AsMemory(), SocketFlags.None);
                            if (length > 0)
                            {
                                token.Data.Data = e.Buffer.AsMemory(0, length);
                                await Receive(token.Data);
                                token.Data.Data = Helper.EmptyArray;
                            }
                        }
                    }

                    if (token.TargetSocket.Connected == false)
                    {
                        await CloseClientSocket(e);
                        return;
                    }
                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        TargetProcessReceive(e);
                    }
                }
                else
                {
                    await CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                await CloseClientSocket(e);
                Logger.Instance.DebugError(ex);
            }
        }
      
        private async Task Receive(Socks5Info info)
        {
            await Semaphore.WaitAsync();
            await socks5MessengerSender.Response(info);
            Semaphore.Release();
        }

        private async Task CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncServerUserToken token = e.UserToken as AsyncServerUserToken;
            if (token.IsClosed == false)
            {
                token.Clear();

                e.Dispose();

                connections.TryRemove(token.Key, out _);
                //maxNumberAcceptedClients.Release();
                await socks5MessengerSender.ResponseClose(token.Data);
            }
        }
        private void CloseClientSocket(AsyncServerUserToken token)
        {
            token.IsClosed = true;
            token.Clear();
            connections.TryRemove(token.Key, out _);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AsyncServerUserToken
    {
        /// <summary>
        /// 
        /// </summary>
        public ConnectionKey Key { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socket TargetSocket { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5Info Data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsClosed { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public byte[] PoolBuffer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            TargetSocket?.SafeClose();
            //TargetSocket = null;

            PoolBuffer = Helper.EmptyArray;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionKeyComparer : IEqualityComparer<ConnectionKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ConnectionKey x, ConnectionKey y)
        {
            return x.RequestId == y.RequestId && x.ConnectId == y.ConnectId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(ConnectionKey obj)
        {
            return obj.RequestId.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public readonly struct ConnectionKey
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly uint RequestId { get; }
        /// <summary>
        /// 
        /// </summary>
        public readonly ulong ConnectId { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectId"></param>
        /// <param name="requestId"></param>
        public ConnectionKey(ulong connectId, uint requestId)
        {
            ConnectId = connectId;
            RequestId = requestId;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class UdpToken
    {
        public ConnectionKeyUdp Key { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socket TargetSocket { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Socks5Info Data { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] PoolBuffer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();
        /// <summary>
        /// 
        /// </summary>
        public EndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            TargetSocket?.SafeClose();
            PoolBuffer = Helper.EmptyArray;
            GC.Collect();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Update()
        {
            LastTime = DateTimeHelper.GetTimeStamp();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionKeyUdpComparer : IEqualityComparer<ConnectionKeyUdp>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ConnectionKeyUdp x, ConnectionKeyUdp y)
        {
            return x.Source != null && x.Source.Equals(y.Source) && x.ConnectId == y.ConnectId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(ConnectionKeyUdp obj)
        {
            if (obj.Source == null) return 0;
            return obj.Source.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public readonly struct ConnectionKeyUdp
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly IPEndPoint Source { get; }
        /// <summary>
        /// 
        /// </summary>
        public readonly ulong ConnectId { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectId"></param>
        /// <param name="endpoint"></param>
        public ConnectionKeyUdp(ulong connectId, IPEndPoint source)
        {
            ConnectId = connectId;
            Source = source;
        }
    }
}
