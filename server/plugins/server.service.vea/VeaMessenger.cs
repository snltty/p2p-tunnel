using common.libs;
using common.libs.extends;
using common.server;
using common.vea;
using server.messengers.singnin;
using System;
using System.Threading.Tasks;

namespace server.service.vea
{
    /// <summary>
    /// 组网消息
    /// </summary>
    [MessengerIdRange((ushort)VeaSocks5MessengerIds.Min, (ushort)VeaSocks5MessengerIds.Max)]
    public sealed class VeaMessenger : IMessenger
    {
        private readonly Config config;
        private readonly IVeaValidator veaValidator;
        private readonly IClientSignInCaching clientSignInCaching;
        private readonly MessengerSender sender;
        public VeaMessenger(Config config, IVeaValidator veaValidator, IClientSignInCaching clientSignInCaching, MessengerSender sender)
        {
            this.config = config;
            this.veaValidator = veaValidator;
            this.clientSignInCaching = clientSignInCaching;
            this.sender = sender;
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.Network)]
        public void GetNetwork(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId) == false || clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo sign) == false)
            {
                connection.WriteUTF8(new DHCPInfo { IP = config.DefaultIPValue }.ToJson());
                return;
            }
            connection.WriteUTF8(config.GetNetwork(sign.GroupId).ToJson());
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.AddNetwork)]
        public void AddNetwork(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId) == false || clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo sign) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            config.AddNetwork(sign.GroupId, connection.ReceiveRequestWrap.Payload.ToUInt32());
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.AssignIP)]
        public void AssignIP(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId) == false || clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo sign) == false)
            {
                connection.Write((uint)0);
                return;
            }
            connection.Write(config.AssignIP(sign));
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.ModifyIP)]
        public async Task ModifyIP(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId) == false || clientSignInCaching.Get(connection.ConnectId, out SignInCacheInfo sign) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            ulong connectionId = connection.ReceiveRequestWrap.Payload.ToUInt64();
            byte ip = connection.ReceiveRequestWrap.Payload[8..].Span[0];
            uint newIP = config.ModifyIP(sign.GroupId, connectionId, ip);
            if (newIP > 0 && clientSignInCaching.Get(connectionId, out sign))
            {
                await sender.SendOnly(new common.server.model.MessageRequestWrap
                {
                    Connection = sign.Connection,
                    MessengerId = (ushort)VeaSocks5MessengerIds.ModifyIP,
                    Payload = BitConverter.GetBytes(newIP)
                });
            }
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.GetSetting)]
        public async Task GetSetting(IConnection connection)
        {
            Config config1 = await config.ReadConfig();
            config1.DHCP = new System.Collections.Generic.Dictionary<string, DHCPInfo>();
            connection.WriteUTF8(config1.ToJson());
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.Setting)]
        public async Task Setting(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            string jsonStr = connection.ReceiveRequestWrap.Payload.GetUTF8String();
            await config.SaveConfig(jsonStr);

            connection.Write(Helper.TrueArray);
        }
    }
}
