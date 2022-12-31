using client.messengers.clients;
using client.messengers.register;
using common.libs;
using common.server;
using common.server.model;
using common.socks5;
using System;

namespace client.service.socks5
{
    /// <summary>
    /// socks5消息发送
    /// </summary>
    public class Socks5MessengerSender : ISocks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly common.socks5.Config config;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;

        private IConnection connection;
        private string targetName;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        /// <param name="config"></param>
        /// <param name="registerStateInfo"></param>
        /// <param name="clientInfoCaching"></param>
        public Socks5MessengerSender(MessengerSender messengerSender, common.socks5.Config config, RegisterStateInfo registerStateInfo, IClientInfoCaching clientInfoCaching)
        {
            this.messengerSender = messengerSender;
            this.config = config;
            this.registerStateInfo = registerStateInfo;
            this.clientInfoCaching = clientInfoCaching;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool Request(Socks5Info data)
        {
            GetConnection();
            byte[] bytes = data.ToBytes(out int length);
            bool res = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Request,
                Connection = connection,
                Payload = bytes.AsMemory(0, length)
            }).Result;
            data.Return(bytes);
            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        public void Response(Socks5Info data)
        {
            byte[] bytes = data.ToBytes(out int length);
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Response,
                Connection = (data.Tag as IConnection).FromConnection,
                Payload = bytes.AsMemory(0, length)
            }).Result;
            data.Return(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public void ResponseClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            Response(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        public void RequestClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            Request(data);
        }

        private void GetConnection()
        {
            if (connection == null || connection.Connected == false || config.TargetName != targetName)
            {
                targetName = config.TargetName;
                if (string.IsNullOrWhiteSpace(config.TargetName))
                {
                    connection = registerStateInfo.OnlineConnection;
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
