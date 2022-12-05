using common.libs.database;
using common.libs.extends;
using common.server.model;
using server.messengers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace server.service.validators
{
    /// <summary>
    /// 
    /// </summary>
    [Table("service-auth-groups")]
    public sealed class JsonFileServiceAccessValidator : IServiceAccessValidator
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, EnumServiceAccess> Groups { get; set; } = new Dictionary<string, EnumServiceAccess>();

        private readonly IConfigDataProvider<JsonFileServiceAccessValidator> configDataProvider;
        /// <summary>
        /// 
        /// </summary>
        public JsonFileServiceAccessValidator() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configDataProvider"></param>
        public JsonFileServiceAccessValidator(IConfigDataProvider<JsonFileServiceAccessValidator> configDataProvider)
        {
            this.configDataProvider = configDataProvider;
            JsonFileServiceAccessValidator config = configDataProvider.Load().Result;
            if (config != null)
            {
                Groups = config.Groups;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="group"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public bool Validate(string group, EnumServiceAccess service)
        {
            if (Groups.TryGetValue(group, out EnumServiceAccess value))
            {
                return (value & service) == service;
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadString()
        {
            return await configDataProvider.LoadString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task SaveConfig()
        {
            await configDataProvider.Save(this);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task SaveConfig(string jsonStr)
        {
            JsonFileServiceAccessValidator _config = jsonStr.DeJson<JsonFileServiceAccessValidator>();
            Groups = _config.Groups;

            await configDataProvider.Save(jsonStr);
        }
    }
}
