using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace common.udpforward
{
    public class UdpForwardResolver
    {
        private ConcurrentDictionary<ConnectionKeyUdp, UdpToken> connections = new(new ConnectionKeyUdpComparer());
        private readonly UdpForwardMessengerSender udpForwardMessengerSender;

        private readonly Config config;
        private readonly WheelTimer<object> wheelTimer;

        public UdpForwardResolver(UdpForwardMessengerSender udpForwardMessengerSender, Config config, WheelTimer<object> wheelTimer)
        {
            //B接收到A的请求
            this.udpForwardMessengerSender = udpForwardMessengerSender;
            udpForwardMessengerSender.OnRequestHandler.Sub(OnRequest);

            this.config = config;
            this.wheelTimer = wheelTimer;

            TimeoutUdp();
        }

        private void OnRequest(UdpForwardInfo arg)
        {
            if (config.ConnectEnable)
            {
                ConnectionKeyUdp key = new ConnectionKeyUdp(arg.Connection.ConnectId, arg.SourceEndpoint);
                if (!connections.TryGetValue(key, out UdpToken token))
                {
                    IPEndPoint endpoint = NetworkHelper.EndpointFromArray(arg.TargetEndpoint);
                    if (!config.LanConnectEnable && endpoint.IsLan())
                    {
                        return;
                    }

                    if (config.PortBlackList.Contains(endpoint.Port))
                    {
                        return;
                    }
                    if (config.PortWhiteList.Length > 0 && !config.PortBlackList.Contains(endpoint.Port))
                    {
                        return;
                    }
                    Socket socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    token = new UdpToken { Connection = arg.Connection, Data = arg, TargetSocket = socket, };

                    token.TargetEP = endpoint;
                    token.PoolBuffer = ArrayPool<byte>.Shared.Rent(65535);
                    connections.AddOrUpdate(key, token, (a, b) => token);

                    _ = token.TargetSocket.SendTo(arg.Buffer.Span, endpoint);
                    token.Data.Buffer = Helper.EmptyArray;
                    IAsyncResult result = socket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
                }
                else
                {
                    _ = token.TargetSocket.SendTo(arg.Buffer.Span, token.TargetEP);
                    token.Data.Buffer = Helper.EmptyArray;
                }
                token.Update();
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

                    var tokens = connections.Where(c => time - c.Value.LastTime > (5 * 60 * 1000));
                    foreach (var item in tokens)
                    {
                        item.Value.Clear();
                        connections.TryRemove(item.Key, out _);
                    }
                }
            }, 1000, true);
        }
        private void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                UdpToken token = result.AsyncState as UdpToken;

                int length = token.TargetSocket.EndReceiveFrom(result, ref token.TempRemoteEP);
                if (length > 0)
                {
                    token.Data.Buffer = token.PoolBuffer.AsMemory(0, length);

                    token.Update();
                    _ = udpForwardMessengerSender.SendResponse(token.Data);
                    token.Data.Buffer = Helper.EmptyArray;
                }
                result = token.TargetSocket.BeginReceiveFrom(token.PoolBuffer, 0, token.PoolBuffer.Length, SocketFlags.None, ref token.TempRemoteEP, ReceiveCallbackUdp, token);
            }
            catch (Exception)
            {
            }
        }
    }

    public class UdpToken
    {
        public IConnection Connection { get; set; }
        public Socket TargetSocket { get; set; }
        public EndPoint TargetEP { get; set; }
        public UdpForwardInfo Data { get; set; }
        public byte[] PoolBuffer { get; set; }

        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();
        public EndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

        public void Clear()
        {
            TargetSocket?.SafeClose();
            if (PoolBuffer != null && PoolBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(PoolBuffer);
            }
            GC.Collect();
            GC.SuppressFinalize(this);
        }

        public void Update()
        {
            LastTime = DateTimeHelper.GetTimeStamp();
        }
    }
    public class ConnectionKeyUdpComparer : IEqualityComparer<ConnectionKeyUdp>
    {
        public bool Equals(ConnectionKeyUdp x, ConnectionKeyUdp y)
        {
            return x.Endpoint.Equals(y.Endpoint) && x.ConnectId == y.ConnectId;
        }

        public int GetHashCode(ConnectionKeyUdp obj)
        {
            return obj.Endpoint.GetHashCode() ^ obj.ConnectId.GetHashCode();
        }
    }
    public readonly struct ConnectionKeyUdp
    {
        public readonly IPEndPoint Endpoint { get; }
        public readonly ulong ConnectId { get; }

        public ConnectionKeyUdp(ulong connectId, IPEndPoint endpoint)
        {
            ConnectId = connectId;
            Endpoint = endpoint;
        }
    }
}
