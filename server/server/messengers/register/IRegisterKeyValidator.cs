using common.server;
using common.server.model;
using System;

namespace server.messengers.register
{
    public interface IRegisterKeyValidator
    {
        public bool Validate(IConnection connection, RegisterParamsInfo registerParamsInfo)
        {
            return true;
        }
    }

    public class DefaultRegisterKeyValidator : IRegisterKeyValidator
    {
    }
}
