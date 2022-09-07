using common.libs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace common.udpforward
{
    public class UdpForwardServer : IUdpForwardServer
    {
        public SimpleSubPushHandler<UdpForwardInfo> OnRequest { get; } = new SimpleSubPushHandler<UdpForwardInfo>();
        public SimpleSubPushHandler<UdpforwardListenChangeInfo> OnListenChange { get; } = new SimpleSubPushHandler<UdpforwardListenChangeInfo>();

        private readonly ServersManager serversManager = new ServersManager();
        private readonly ClientsManager clientsManager = new ClientsManager();

        public UdpForwardServer(WheelTimer<object> wheelTimer)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                State = null,
                Callback = clientsManager.Timeout
            }, 1000, true);
        }

        public void Start(int sourcePort)
        {
            if (serversManager.Contains(sourcePort))
                return;



            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, sourcePort);
            var udpClient = new UdpClient(localEndPoint);

            serversManager.TryAdd(new ForwardAsyncServerToken { SourcePort = sourcePort, UdpClient = udpClient });
            IAsyncResult result = udpClient.BeginReceive(ProcessReceiveUdp, new ForwardAsyncServerToken { SourcePort = sourcePort, UdpClient = udpClient });

            OnListenChange.Push(new UdpforwardListenChangeInfo { Port = sourcePort, State = true });
        }
        private void ProcessReceiveUdp(IAsyncResult result)
        {
            try
            {
                var token = result.AsyncState as ForwardAsyncServerToken;
                var data = token.UdpClient.EndReceive(result, ref token.TempRemoteEP);
                if (!clientsManager.TryGetValue(token.TempRemoteEP, out ForwardAsyncUserToken userToken))
                {
                    userToken = new ForwardAsyncUserToken
                    {
                        ServerUdpClient = token.UdpClient,
                        Request = new UdpForwardInfo { SourceEndpoint = token.TempRemoteEP, SourcePort = token.SourcePort }
                    };
                    clientsManager.TryAdd(userToken);
                }
                userToken.Request.Buffer = data;
                OnRequest.Push(userToken.Request);
                userToken.Request.Buffer = Helper.EmptyArray;

                result = token.UdpClient.BeginReceive(ProcessReceiveUdp, token);
            }
            catch (Exception)
            {
            }
        }

        public void Response(UdpForwardInfo model)
        {
            if (clientsManager.TryGetValue(model.SourceEndpoint, out ForwardAsyncUserToken token))
            {
                var span = model.Buffer.Span;
                if (span.Length > 0)
                {
                    try
                    {
                        token.ServerUdpClient.Send(span, model.SourceEndpoint);
                    }
                    catch (Exception)
                    {
                        clientsManager.TryRemove(model.SourceEndpoint, out _);
                    }
                }
                else
                {
                    clientsManager.TryRemove(model.SourceEndpoint, out _);
                }
            }
        }

        public void Stop(int sourcePort)
        {
            if (serversManager.TryRemove(sourcePort, out ForwardAsyncServerToken model))
            {
                clientsManager.Clear(model.SourcePort);
                OnListenChange.Push(new UdpforwardListenChangeInfo { Port = model.SourcePort, State = false });
            }

        }
        public void Stop()
        {
            serversManager.Clear();
            clientsManager.Clear();
            OnListenChange.Push(new UdpforwardListenChangeInfo { Port = 0, State = false });
        }

    }

    public class ForwardAsyncUserToken
    {
        public UdpClient ServerUdpClient { get; set; }
        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();
        public UdpForwardInfo Request { get; set; } = new UdpForwardInfo();
    }
    public class ClientsManager
    {
        private ConcurrentDictionary<IPEndPoint, ForwardAsyncUserToken> clients = new(new IPEndPointDictionaryComparer());

        public bool Contains(IPEndPoint ep)
        {
            return clients.ContainsKey(ep);
        }
        public bool TryAdd(ForwardAsyncUserToken model)
        {
            return clients.TryAdd(model.Request.SourceEndpoint, model);
        }
        public bool TryGetValue(IPEndPoint ep, out ForwardAsyncUserToken c)
        {
            return clients.TryGetValue(ep, out c);
        }
        public bool TryRemove(IPEndPoint ep, out ForwardAsyncUserToken c)
        {
            return clients.TryRemove(ep, out c);
        }
        public void Clear(int sourcePort)
        {
            IEnumerable<IPEndPoint> requestIds = clients.Where(c => c.Value.Request.SourcePort == sourcePort).Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                TryRemove(requestId, out _);
            }
        }
        public void Clear()
        {
            IEnumerable<IPEndPoint> requestIds = clients.Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                TryRemove(requestId, out _);
            }
        }
        public void Timeout(WheelTimerTimeout<object> timeout)
        {
            long time = DateTimeHelper.GetTimeStamp();

            var tokens = clients.Where(c => time - c.Value.LastTime > (5 * 60 * 1000));
            foreach (var item in tokens)
            {
                clients.TryRemove(item.Key, out _);
            }
        }
    }
    public class ForwardAsyncServerToken
    {
        public int SourcePort { get; set; }
        public IPEndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        public UdpClient UdpClient { get; set; }
    }
    public class ServersManager
    {
        public ConcurrentDictionary<int, ForwardAsyncServerToken> services = new();
        public bool TryAdd(ForwardAsyncServerToken model)
        {
            return services.TryAdd(model.SourcePort, model);
        }
        public bool Contains(int port)
        {
            return services.ContainsKey(port);
        }
        public bool TryGetValue(int port, out ForwardAsyncServerToken c)
        {
            return services.TryGetValue(port, out c);
        }
        public bool TryRemove(int port, out ForwardAsyncServerToken c)
        {
            bool res = services.TryRemove(port, out c);
            if (res)
            {
                try
                {
                    c.UdpClient.Dispose();
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
                    item.UdpClient.Dispose();
                    GC.Collect();
                }
                catch (Exception)
                {
                }
            }
            services.Clear();
        }

    }

}