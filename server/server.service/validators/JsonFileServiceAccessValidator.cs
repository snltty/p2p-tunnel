using common.libs.database;
using server.messengers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.service.validators
{
    [Table("service-auth-groups")]
    public class JsonFileServiceAccessValidator : IServiceAccessValidator
    {
        public Dictionary<string, EnumService> Groups { get; init; } = new Dictionary<string, EnumService>();

        public JsonFileServiceAccessValidator() { }
        public JsonFileServiceAccessValidator(IConfigDataProvider<JsonFileServiceAccessValidator> configDataProvider)
        {
            JsonFileServiceAccessValidator config = configDataProvider.Load().Result;
            if (config != null)
            {
                Groups = config.Groups;
            }
        }

        public bool Validate(string group, EnumService service)
        {
            if (Groups.TryGetValue(group, out EnumService value))
            {
                return (value & service) == service;
            }
            return false;
        }
    }
}
