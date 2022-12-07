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
    /// <summary>
    /// socks5服务端配置文件
    /// </summary>
    public sealed class Socks5ServerConfigure : IClientConfigure
    {
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerStateInfo;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        /// <param name="registerStateInfo"></param>
        public Socks5ServerConfigure(MessengerSender messengerSender, RegisterStateInfo registerStateInfo)
        {
            this.messengerSender = messengerSender;
            this.registerStateInfo = registerStateInfo;
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name => "Socks5服务端";

        /// <summary>
        /// 作者
        /// </summary>
        public string Author => "snltty";

        /// <summary>
        /// 描述
        /// </summary>
        public string Desc => "";

        /// <summary>
        /// 启用
        /// </summary>
        public bool Enable => true;

        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        public async Task<string> Load()
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.GetSetting,
                Connection = registerStateInfo.OnlineConnection,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.GetString();
            }
            return string.Empty;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
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
