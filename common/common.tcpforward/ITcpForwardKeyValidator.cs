namespace common.tcpforward
{
    public interface ITcpForwardKeyValidator
    {
        public bool Validate(TcpForwardInfo arg);
    }

    public class DefaultTcpForwardKeyValidator : ITcpForwardKeyValidator
    {

        public DefaultTcpForwardKeyValidator()
        {

        }
        public bool Validate(TcpForwardInfo arg)
        {
            return true;
        }
    }

}
