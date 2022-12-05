using common.libs;
using common.server;
using common.server.model;

namespace server.service.messengers
{
    /// <summary>
    /// 
    /// </summary>
    [MessengerIdRange((ushort)HeartMessengerIds.Min, (ushort)HeartMessengerIds.Max)]
    public sealed class HeartMessenger : IMessenger
    {
        /// <summary>
        /// 
        /// </summary>
        public HeartMessenger()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)HeartMessengerIds.Alive)]
        public byte[] Alive(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}
