using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.server.model
{
    internal class HeartInfo
    {
    }

    [Flags, MessengerIdEnum]
    public enum HeartMessengerIds : int
    {
        Min = 301,
        Alive = 301,
        Max = 400,
    }
}
