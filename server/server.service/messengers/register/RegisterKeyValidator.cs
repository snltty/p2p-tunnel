using common.server.model;
using common.server;
using server.messengers.register;
using System;
using System.Linq;

namespace server.service.messengers.register
{
    public class RegisterKeyValidator : IRegisterKeyValidator
    {
        private readonly KeysConfig keysConfig;
        public RegisterKeyValidator(KeysConfig keysConfig)
        {
            this.keysConfig = keysConfig;
        }
        public bool Validate(IConnection connection, RegisterParamsInfo registerParamsInfo)
        {
            return keysConfig.Keys.Length == 0 || keysConfig.Keys.Contains(registerParamsInfo.Key);
        }
    }


}
