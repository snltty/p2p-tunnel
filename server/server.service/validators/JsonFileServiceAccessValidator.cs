using common.libs.database;
using server.messengers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.service.validators
{
    [Table("service-auth-groups")]
    public class JsonFileServiceAccessValidator : IServiceAccessValidator
    {
        private Dictionary<string, EnumService> Groups { get; set; } = new Dictionary<string, EnumService>();
        public JsonFileServiceAccessValidator() { }
        public JsonFileServiceAccessValidator(IConfigDataProvider<JsonFileServiceAccessValidator> configDataProvider)
        {
            JsonFileServiceAccessValidator config = configDataProvider.Load().Result;
            if (config != null)
            {
                Groups = config.Groups;
            }
        }

        public bool Validate(string key, EnumService service)
        {
            if (Groups.TryGetValue(key, out EnumService value))
            {
                return (value & service) == service;
            }
            return false;
        }
    }
}
