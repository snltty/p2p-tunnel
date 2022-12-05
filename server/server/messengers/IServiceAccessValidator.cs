using common.server.model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.messengers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IServiceAccessValidator
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, EnumServiceAccess> Groups { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="group"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public bool Validate(string group, EnumServiceAccess service);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<string> ReadString();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task SaveConfig();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public Task SaveConfig(string jsonStr);
    }

}
