using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class WakeUpClientService : IClientService
    {
        private readonly Config config;
        private readonly WakeUpTransfer wakeUpTransfer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="wakeUpTransfer"></param>
        public WakeUpClientService(Config config, WakeUpTransfer wakeUpTransfer)
        {
            this.config = config;
            this.wakeUpTransfer = wakeUpTransfer;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public Config Get(ClientServiceParamsInfo arg)
        {
            return config;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public Dictionary<string, List<ConfigItem>> List(ClientServiceParamsInfo arg)
        {
            return wakeUpTransfer.Get();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> WakeUp(ClientServiceParamsInfo arg)
        {
            WakeUpModel model = arg.Content.DeJson<WakeUpModel>();
            return await wakeUpTransfer.WakeUp(model.Name, model.Mac);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Add(ClientServiceParamsInfo arg)
        {
            AddModel model = arg.Content.DeJson<AddModel>();
            return await config.Add(model.Index, model.Item);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Remove(ClientServiceParamsInfo arg)
        {
            RemoveModel model = arg.Content.DeJson<RemoveModel>();
            return await config.Remove(model.Index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public bool Update(ClientServiceParamsInfo arg)
        {
            wakeUpTransfer.UpdateMac();
            return true;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class WakeUpModel
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Mac { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class AddModel
    {
        /// <summary>
        /// 
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ConfigItem Item { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class RemoveModel
    {
        /// <summary>
        /// 
        /// </summary>
        public int Index { get; set; }
    }
}
