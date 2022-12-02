using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.socks5;
using System;
using System.Threading.Tasks;

namespace client.service.udpforward
{
    public sealed class Socks5ServerConfigure : IClientConfigure
    {
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerStateInfo;

        public Socks5ServerConfigure(MessengerSender messengerSender, RegisterStateInfo registerStateInfo)
        {
            this.messengerSender = messengerSender;
            this.registerStateInfo = registerStateInfo;
        }

        public string Name => "Socks5服务端";

        public string Author => "snltty";

        public string Desc => "";

        public bool Enable => true;

        public async Task<string> Load()
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.GetSetting,
                Connection = registerStateInfo.OnlineConnection,
                Payload = Helper.EmptyArray
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.GetString();
            }
            return string.Empty;
        }

        public async Task<string> Save(string jsonStr)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Setting,
                Connection = registerStateInfo.OnlineConnection,
                Payload = jsonStr.ToBytes()
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray))
            {
                return string.Empty;
            }
            return "配置失败";
        }
    }
}
