using System.Net;
using System;
using common.libs.extends;

namespace common.socks5
{
    public interface ISocks5Validator
    {
        public bool Validate(Socks5Info info, Config config);
    }

    public class DefaultSocks5Validator : ISocks5Validator
    {

        public DefaultSocks5Validator()
        {

        }
        public bool Validate(Socks5Info info, Config config)
        {
            if (config.ConnectEnable == false)
            {
                return false;
            }

            IPEndPoint remoteEndPoint = Socks5Parser.GetRemoteEndPoint(info.Data, out Span<byte> ipMemory);
            if (config.LanConnectEnable == false && remoteEndPoint.IsLan())
            {
                return false;
            }

            return true;
        }
    }

}
