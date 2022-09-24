using common.libs;
using common.server;
using System;

namespace client.realize.messengers.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        public byte[] Execute(IConnection connection)
        {
            return Helper.TrueArray;
        }

        public byte[] Alive(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}
