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
    public sealed class Socks5MessengerSender : ISocks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        private readonly common.socks5.Config config;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly IClientsTransfer clientsTransfer;

        private IConnection connection;
        private string targetName;
        public Socks5MessengerSender(MessengerSender messengerSender, common.socks5.Config config, SignInStateInfo signInStateInfo, IClientInfoCaching clientInfoCaching, IClientsTransfer clientsTransfer)
        {
            this.messengerSender = messengerSender;
            this.config = config;
            this.signInStateInfo = signInStateInfo;
            this.clientInfoCaching = clientInfoCaching;
            this.clientsTransfer = clientsTransfer;
        }

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
        public async Task ResponseClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            await Response(data);
        }

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
                        if(connection == null || connection.Connected == false)
                        {
                            clientsTransfer.ConnectClient(client);
                        }
                    }
                }
            }
        }
    }
}
