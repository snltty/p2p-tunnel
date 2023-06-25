using client.messengers.singnin;
using client.service.ui.api.clientServer;
using common.libs;
using common.libs.extends;
using common.server;
using common.vea;
using System;
using System.Threading.Tasks;

namespace client.service.vea.server
{
    public sealed class ServerVeaClientService : IClientService
    {
        private readonly MessengerSender messengerSender;
        private readonly SignInStateInfo signInStateInfo;

        public ServerVeaClientService(MessengerSender messengerSender, SignInStateInfo signInStateInfo)
        {
            this.messengerSender = messengerSender;
            this.signInStateInfo = signInStateInfo;
        }

        public async Task<DHCPInfo> List(ClientServiceParamsInfo arg)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new common.server.model.MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)common.vea.VeaSocks5MessengerIds.Network,
            });
            if (resp.Code == common.server.model.MessageResponeCodes.OK)
            {
                return resp.Data.GetUTF8String().DeJson<DHCPInfo>();
            }
            return null;
        }

        public async Task<bool> AddNetwork(ClientServiceParamsInfo arg)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new common.server.model.MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)common.vea.VeaSocks5MessengerIds.AddNetwork,
                Payload = BitConverter.GetBytes(uint.Parse(arg.Content))
            });
            if (resp.Code == common.server.model.MessageResponeCodes.OK)
            {
                return resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;
        }
        public async Task<bool> ModifyIP(ClientServiceParamsInfo arg)
        {
            ModifyIPModel model = arg.Content.DeJson<ModifyIPModel>();
            MessageResponeInfo resp = await messengerSender.SendReply(new common.server.model.MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)common.vea.VeaSocks5MessengerIds.ModifyIP,
                Payload = model.ToBytes()
            });
            if (resp.Code == common.server.model.MessageResponeCodes.OK)
            {
                return resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;
        }
        public async Task<bool> DeleteIP(ClientServiceParamsInfo arg)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new common.server.model.MessageRequestWrap
            {
                Connection = signInStateInfo.Connection,
                MessengerId = (ushort)common.vea.VeaSocks5MessengerIds.DeleteIP,
                Payload = BitConverter.GetBytes(ulong.Parse(arg.Content))
            });
            if (resp.Code == common.server.model.MessageResponeCodes.OK)
            {
                return resp.Data.Span.SequenceEqual(Helper.TrueArray);
            }
            return false;
        }
    }

    public sealed class ModifyIPModel
    {
        public ulong ConnectionId { get; set; }
        public byte IP { get; set; }

        public byte[] ToBytes()
        {
            byte[] bytes = new byte[8 + 1];
            ConnectionId.ToBytes(bytes);
            bytes[8] = IP;
            return bytes;
        }
    }
}
