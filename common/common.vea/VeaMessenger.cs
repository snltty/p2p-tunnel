using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Threading.Tasks;

namespace common.vea
{
    /// <summary>
    /// 组网消息
    /// </summary>
    [MessengerIdRange((ushort)VeaSocks5MessengerIds.Min, (ushort)VeaSocks5MessengerIds.Max)]
    public sealed class VeaMessenger : IMessenger
    {
        private readonly Config config;
        private readonly IVeaAccessValidator veaValidator;
        private readonly MessengerSender sender;
        public VeaMessenger(Config config, IVeaAccessValidator veaValidator, MessengerSender sender)
        {
            this.config = config;
            this.veaValidator = veaValidator;
            this.sender = sender;
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.Network)]
        public void GetNetwork(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId, out VeaAccessValidateResult result) == false)
            {
                connection.WriteUTF8(new DHCPInfo { IP = config.DefaultIPValue }.ToJson());
                return;
            }
            DHCPInfo info = config.GetNetwork(result.Key);
            foreach (var item in info.Assigned)
            {
                item.Value.OnLine = item.Value.Connection != null && item.Value.Connection.Connected;
            }
            connection.WriteUTF8(info.ToJson());
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.AddNetwork)]
        public void AddNetwork(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId, out VeaAccessValidateResult result) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            config.AddNetwork(result.Key, connection.ReceiveRequestWrap.Payload.ToUInt32(), (assign) =>
            {
                if (assign.Connection != null && assign.Connection.Connected)
                {
                    _ = sender.SendOnly(new common.server.model.MessageRequestWrap
                    {
                        Connection = assign.Connection,
                        MessengerId = (ushort)VeaSocks5MessengerIds.ModifiedIP,
                        Payload = BitConverter.GetBytes(assign.IP)
                    });
                }
            });
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.AssignIP)]
        public void AssignIP(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId, out VeaAccessValidateResult result) == false)
            {
                connection.Write((uint)0);
                return;
            }
            connection.Write(config.AssignIP(result.Key, connection.ReceiveRequestWrap.Payload.Span[0], connection, result.Name));
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.ModifyIP)]
        public async Task ModifyIP(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId, out VeaAccessValidateResult result) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            ulong connectionId = connection.ReceiveRequestWrap.Payload.ToUInt64();
            byte ip = connection.ReceiveRequestWrap.Payload[8..].Span[0];

            uint newIP = config.ModifyIP(result.Key, connectionId, ip);
            if (newIP > 0)
            {
                await sender.SendOnly(new common.server.model.MessageRequestWrap
                {
                    Connection = result.Connection,
                    MessengerId = (ushort)VeaSocks5MessengerIds.ModifiedIP,
                    Payload = BitConverter.GetBytes(newIP)
                });
            }
            connection.Write(Helper.TrueArray);
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.DeleteIP)]
        public void DeleteIP(IConnection connection)
        {
            if (veaValidator.Validate(connection.ConnectId, out VeaAccessValidateResult result) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            ulong connectionId = connection.ReceiveRequestWrap.Payload.ToUInt64();
            config.DeleteIP(result.Key, connectionId);
            connection.Write(Helper.TrueArray);
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
            if (veaValidator.Validate(connection.ConnectId, out VeaAccessValidateResult result) == false)
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
