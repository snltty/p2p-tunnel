using client.messengers.clients;
using client.messengers.singnin;
using common.libs;
using common.server;
using common.server.model;
using common.socks5;
using System;
using System.Threading.Tasks;

namespace client.service.socks5
{
    /// <summary>
    /// socks5消息发送
    /// </summary>
    public class Socks5MessengerSender : ISocks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly common.socks5.Config config;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;

        private IConnection connection;
        private string targetName;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        /// <param name="config"></param>
        /// <param name="signInStateInfo"></param>
        /// <param name="clientInfoCaching"></param>
        public Socks5MessengerSender(MessengerSender messengerSender, common.socks5.Config config, SignInStateInfo signInStateInfo, IClientInfoCaching clientInfoCaching)
        {
            this.messengerSender = messengerSender;
            this.config = config;
            this.signInStateInfo = signInStateInfo;
            this.clientInfoCaching = clientInfoCaching;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> Request(Socks5Info data)
        {
            GetConnection();
            if (connection == null || connection.Connected == false) return false;

            byte[] bytes = data.ToBytes(out int length);
            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Request,
                Connection = connection,
                Payload = bytes.AsMemory(0, length)
            });
            data.Return(bytes);
            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        public async Task<bool> Response(Socks5Info data)
        {
            byte[] bytes = data.ToBytes(out int length);
            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Response,
                Connection = (data.Tag as IConnection).FromConnection,
                Payload = bytes.AsMemory(0, length)
            });
            data.Return(bytes);
            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public async Task ResponseClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            await Response(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public async Task RequestClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            await Request(data);
        }

        private void GetConnection()
        {
            if (connection == null || connection.Connected == false || config.TargetName != targetName)
            {
                targetName = config.TargetName;
                if (config.TargetName == "/")
                {
                    connection = signInStateInfo.Connection;
                }
                else
                {
                    if (clientInfoCaching.GetByName(config.TargetName, out ClientInfo client))
                    {
                        connection = client.Connection;
                    }
                }
            }
        }
    }
}
