using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.forward;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.service.forward.model;
using System;
using System.Threading.Tasks;

namespace client.service.forward.server
{
    /// <summary>
    /// tcp转发服务端配置文件
    /// </summary>
    public sealed class ServerForwardConfigure : IClientConfigure
    {
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInStateInfo;
        public ServerForwardConfigure(MessengerSender messengerSender, SignInStateInfo signInStateInfo)
        {
            this.messengerSender = messengerSender;
            this.signInStateInfo = signInStateInfo;
        }

        public string Name => "端口转发服务端";
        public string Author => "snltty";
        public string Desc => "白名单不为空时只允许白名单内端口";
        public bool Enable => true;

        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        public async Task<string> Load()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ForwardMessengerIds.GetSetting,
                Connection = signInStateInfo.Connection,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.GetUTF8String();
            }
            return string.Empty;
        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public async Task<bool> Save(string jsonStr)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ForwardMessengerIds.Setting,
                Connection = signInStateInfo.Connection,
                Payload = jsonStr.ToUTF8Bytes()
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
    }
}
