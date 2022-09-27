namespace common.udpforward
{
    public interface IUdpForwardKeyValidator
    {
        public bool Validate(UdpForwardInfo info);
    }

    public class DefaultUdpForwardKeyValidator : IUdpForwardKeyValidator
    {

        public DefaultUdpForwardKeyValidator()
        {

        }
        public bool Validate(UdpForwardInfo info)
        {
            return true;
        }
    }

}
