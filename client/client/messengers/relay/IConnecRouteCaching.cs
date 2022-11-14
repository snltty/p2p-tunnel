using common.server.model;

namespace client.messengers.relay
{
    public interface IConnecRouteCaching
    {
        public void AddConnects(ConnectsInfo connects);
        public void AddRoutes(RoutesInfo routes);

        public void Remove(ulong id);
        public void Clear();
    }
}
