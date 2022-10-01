using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs.extends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    public class WakeUpClientService : IClientService
    {
        private readonly Config config;
        private readonly WakeUpTransfer wakeUpTransfer;

        public WakeUpClientService(Config config, WakeUpTransfer wakeUpTransfer)
        {
            this.config = config;
            this.wakeUpTransfer = wakeUpTransfer;
        }

        public Config Get(ClientServiceParamsInfo arg)
        {
            return config;
        }

        public Dictionary<string, List<ConfigItem>> Update(ClientServiceParamsInfo arg)
        {
            return wakeUpTransfer.Get();
        }
        public async Task<bool> WakeUp(ClientServiceParamsInfo arg)
        {
            WakeUpModel model = arg.Content.DeJson<WakeUpModel>();
            return await wakeUpTransfer.WakeUp(model.Name, model.Mac);
        }

        public async Task<bool> Add(ClientServiceParamsInfo arg)
        {
            AddModel model = arg.Content.DeJson<AddModel>();
            return await config.Add(model.Index, model.Item);
        }
        public async Task<bool> Remove(ClientServiceParamsInfo arg)
        {
            RemoveModel model = arg.Content.DeJson<RemoveModel>();
            return await config.Remove(model.Index);
        }
    }

    public class WakeUpModel
    {
        public string Name { get; set; }
        public string Mac { get; set; }
    }

    public class AddModel
    {
        public int Index { get; set; }
        public ConfigItem Item { get; set; }
    }

    public class RemoveModel
    {
        public int Index { get; set; }
    }
}
