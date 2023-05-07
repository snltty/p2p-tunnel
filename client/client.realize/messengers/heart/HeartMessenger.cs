using common.libs;
using common.server;
using common.server.model;

namespace client.realize.messengers.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    [MessengerIdRange((ushort)HeartMessengerIds.Min, (ushort)HeartMessengerIds.Max)]
    public sealed class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        /// <summary>
        /// 活着
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)HeartMessengerIds.Alive)]
        public void Alive(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)HeartMessengerIds.Test)]
        public void Test(IConnection connection)
        {
        }
    }
}
