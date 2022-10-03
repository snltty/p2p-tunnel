using common.libs.database;
using server.messengers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.service.validators
{
    [Table("service-auth-keys")]
    public class JsonFileServiceAccessValidator : IServiceAccessValidator
    {
        public Dictionary<string, EnumService> Keys { get; set; } = new Dictionary<string, EnumService>();
        public JsonFileServiceAccessValidator() { }
        public JsonFileServiceAccessValidator(IConfigDataProvider<JsonFileServiceAccessValidator> configDataProvider)
        {
            JsonFileServiceAccessValidator config = configDataProvider.Load().Result;
            if (config != null)
            {
                Keys = config.Keys;
            }
        }

        public bool Validate(string key, EnumService service)
        {
            if (Keys.TryGetValue(key, out EnumService value))
            {
                return (value & service) == service;
            }
            return false;
        }
    }
}
