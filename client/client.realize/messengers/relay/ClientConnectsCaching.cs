using client.messengers.relay;
using common.server.model;
using System.Collections.Concurrent;

namespace client.realize.messengers.relay
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ClientConnectsCaching : IClientConnectsCaching
    {
        ConcurrentDictionary<ulong, ulong[]> connectsDic = new ConcurrentDictionary<ulong, ulong[]>();

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<ulong, ulong[]> Connects => connectsDic;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connects"></param>
        public void AddConnects(ConnectsInfo connects)
        {
            connectsDic.AddOrUpdate(connects.Id, connects.Connects, (a, b) => connects.Connects);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void Remove(ulong id)
        {
            connectsDic.TryRemove(id, out _);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            connectsDic.Clear();
        }

    }
}
