using client.messengers.clients;
using client.messengers.register;
using client.messengers.relay;
using common.server;
using common.server.model;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace client.realize.messengers.relay
{
    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public SourceConnectionSelector(IClientInfoCaching clientInfoCaching, RegisterStateInfo registerStateInfo, IConnecRouteCaching connecRouteCaching)
        {
            this.clientInfoCaching = clientInfoCaching;

            clientInfoCaching.OnRemove.Sub((client) =>
            {
                connecRouteCaching.Remove(client.Id);
            });
            registerStateInfo.OnRegisterStateChange.Sub((state) =>
            {
                if (state == false)
                {
                    connecRouteCaching.Clear();
                }
            });
        }

        public IConnection SelectSource(IConnection connection, ulong relayid)
        {
            if (relayid > 0)
            {
                if (clientInfoCaching.Get(relayid, out ClientInfo client))
                {
                    return connection.ServerType == common.server.model.ServerType.TCP ? client.TcpConnection : client.UdpConnection;
                }
            }
            return connection;
        }

        public IConnection SelectTarget(IConnection connection, ulong relayid)
        {
            if (relayid > 0)
            {
                if (clientInfoCaching.Get(relayid, out ClientInfo client))
                {
                    return connection.ServerType == common.server.model.ServerType.TCP ? client.TcpConnection : client.UdpConnection;
                }
            }
            return connection;
        }
    }

    public class ConnecRouteCaching : IConnecRouteCaching
    {
        ConcurrentDictionary<ulong, ConnectInfo[]> connectsDic = new ConcurrentDictionary<ulong, ConnectInfo[]>();
        ConcurrentDictionary<RouteKey, RouteInfo> routesDic = new ConcurrentDictionary<RouteKey, RouteInfo>();

        public void AddConnects(ConnectsInfo connects)
        {
            connectsDic.AddOrUpdate(connects.Id, connects.Connects, (a, b) => connects.Connects);
        }

        public void AddRoutes(RoutesInfo routes)
        {
            routesDic.AddOrUpdate(new RouteKey(routes.To.FromId, routes.To.ToId), routes.To, (a, b) => routes.To);
            routesDic.AddOrUpdate(new RouteKey(routes.Back.FromId, routes.Back.ToId), routes.Back, (a, b) => routes.Back);
        }

        public void Remove(ulong id)
        {
            connectsDic.TryRemove(id, out _);

            var keys = routesDic.Where(c => c.Value.ToId == id || c.Value.FromId == id || c.Value.TargetId == id).Select(c => c.Key);
            foreach (var item in keys)
            {
                routesDic.TryRemove(item, out _);
            }
        }
        public void Clear()
        {
            connectsDic.Clear();
            routesDic.Clear();
        }


        public struct RouteKey
        {
            public ulong FromId;
            public ulong ToId;

            public RouteKey(ulong fromid, ulong toid)
            {
                FromId = fromid;
                ToId = toid;
            }
        }

        public class RouteKeyComparer : IEqualityComparer<RouteKey>
        {
            public bool Equals(RouteKey x, RouteKey y)
            {
                return x.FromId == y.FromId && x.ToId == y.ToId;
            }

            public int GetHashCode(RouteKey obj)
            {
                return obj.FromId.GetHashCode() ^ obj.ToId.GetHashCode();
            }
        }
    }
}
