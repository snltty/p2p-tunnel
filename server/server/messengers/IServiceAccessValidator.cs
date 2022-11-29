using common.server.model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.messengers
{
    public interface IServiceAccessValidator
    {
        public Dictionary<string, EnumServiceAccess> Groups { get; set; }
        public bool Validate(string group, EnumServiceAccess service);

        public Task<string> ReadString();
        public Task SaveConfig();
        public Task SaveConfig(string jsonStr);
    }

}
