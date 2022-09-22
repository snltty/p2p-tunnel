using common.server.model;
using common.server;
using server.messengers.register;
using common.libs.database;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace server.service.messengers.register
{
    public class RegisterKeyValidator : IRegisterKeyValidator
    {
        private readonly RegisterKeys registerKeys;
        public RegisterKeyValidator(RegisterKeys registerKeys)
        {
            this.registerKeys = registerKeys;
        }
        public bool Validate(IConnection connection, RegisterParamsInfo registerParamsInfo)
        {
            return registerKeys.Keys.Length == 0 || registerKeys.Keys.Contains(registerParamsInfo.Key);
        }
    }

    [Table("keys")]
    public class RegisterKeys
    {
        public RegisterKeys() { }
        private readonly IConfigDataProvider<RegisterKeys> configDataProvider;
        public RegisterKeys(IConfigDataProvider<RegisterKeys> configDataProvider)
        {
            this.configDataProvider = configDataProvider;

            RegisterKeys config = ReadConfig().Result;
            if(config != null)
            {
                Keys = config.Keys;
            }
        }

        public string[] Keys { get; set; } = Array.Empty<string>();

        private async Task<RegisterKeys> ReadConfig()
        {
            return await configDataProvider.Load();
        }
    }
}
