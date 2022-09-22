using common.server;
using common.server.model;
using System.IO;

namespace server.messengers.register
{
    public interface IRegisterKeyValidator
    {
        public bool Validate(IConnection connection, RegisterParamsInfo registerParamsInfo);
    }

    public class DefaultRegisterKeyValidator : IRegisterKeyValidator
    {

        public DefaultRegisterKeyValidator()
        {

        }
        public bool Validate(IConnection connection, RegisterParamsInfo registerParamsInfo)
        {
            return true;
        }
    }

}
