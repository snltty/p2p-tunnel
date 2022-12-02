using common.libs.database;
using common.server.model;
using server.messengers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace server.service.validators
{
    [Table("service-auth-groups")]
    public sealed class JsonFileServiceAccessValidator : IServiceAccessValidator
    {
        public Dictionary<string, EnumServiceAccess> Groups { get; set; } = new Dictionary<string, EnumServiceAccess>();

        private readonly IConfigDataProvider<JsonFileServiceAccessValidator> configDataProvider;
        public JsonFileServiceAccessValidator() { }
        public JsonFileServiceAccessValidator(IConfigDataProvider<JsonFileServiceAccessValidator> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            JsonFileServiceAccessValidator config = configDataProvider.Load().Result;
            if (config != null)
            {
                Groups = config.Groups;
            }
        }

        public bool Validate(string group, EnumServiceAccess service)
        {
            if (Groups.TryGetValue(group, out EnumServiceAccess value))
            {
                return (value & service) == service;
            }
            return false;
        }
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

        public async Task SaveConfig()
        {
            await configDataProvider.Save(this);
        }
        public async Task SaveConfig(string jsonStr)
        {
            await configDataProvider.Save(jsonStr);
        }
    }
}
