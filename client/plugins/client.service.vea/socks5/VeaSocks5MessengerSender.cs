using common.libs;
using common.server;
using common.server.model;
using common.socks5;
using System;
using System.Threading.Tasks;

namespace client.service.vea.socks5
{
    /// <summary>
    /// 组网socks5消息发送
    /// </summary>
    public interface IVeaSocks5MessengerSender : ISocks5MessengerSender
    {

    }
    /// <summary>
    /// 组网socks5消息发送
    /// </summary>
    public sealed class VeaSocks5MessengerSender : IVeaSocks5MessengerSender
    {
        private readonly MessengerSender messengerSender;
        public VeaSocks5MessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public async Task<bool> Request(Socks5Info data)
        {
            byte[] bytes = data.ToBytes(out int length);
            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)VeaSocks5MessengerIds.Request,
                Connection = (data.Tag as IConnection),
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
                MessengerId = (ushort)VeaSocks5MessengerIds.Response,
                Connection = (data.Tag as IConnection).FromConnection,
                Payload = bytes.AsMemory(0, length)
            });
            data.Return(bytes);
            return res;
        }

        public async Task ResponseClose(Socks5Info data)
        {
            await Response(data);
        }


        public async Task RequestClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            await Request(data);
        }

    }
}
