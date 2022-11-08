using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    public class RegisterParamsInfo
    {
        public RegisterParamsInfo() { }

        /// <summary>
        /// 本地ip，loopback 、LAN ip
        /// </summary>
        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        /// <summary>
        /// 连接id，因为分两次注册，第二次带上第一次的注册后获得的id
        /// </summary>
        public ulong Id { get; set; } = 0;
        /// <summary>
        /// 分组
        /// </summary>
        public string GroupId { get; set; } = string.Empty;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 本机tcp端口
        /// </summary>
        public int LocalTcpPort { get; set; } = 0;
        /// <summary>
        /// 本机udp端口
        /// </summary>
        public int LocalUdpPort { get; set; } = 0;

        /// <summary>
        /// 客户端自定义的权限列表
        /// </summary>
        public uint ClientAccess { get; set; } = 0;

        public byte[] ToBytes()
        {
            int length = 0;

            byte[][] ipBytes = new byte[LocalIps.Length][];
            for (int i = 0; i < LocalIps.Length; i++)
            {
                ipBytes[i] = LocalIps[i].GetAddressBytes();
                length += 1 + ipBytes[i].Length;
            }
            length += 1;

            var idBytes = Id.ToBytes();
            length += 8;
            var groupidBytes = GroupId.ToBytes();
            length += 1 + groupidBytes.Length;
            var nameBytes = Name.ToBytes();
            length += 1 + nameBytes.Length;
            var localtcpPort = LocalTcpPort.ToBytes();
            length += 2;
            var localudpPort = LocalUdpPort.ToBytes();
            length += 2;

            var clientAccess = ClientAccess.ToBytes();
            length += clientAccess.Length;

            var bytes = new byte[length];
            int index = 0;
            bytes[index] = (byte)ipBytes.Length;
            index += 1;
            for (int i = 0; i < ipBytes.Length; i++)
            {
                bytes[index] = (byte)ipBytes[i].Length;
                Array.Copy(ipBytes[i], 0, bytes, index + 1, ipBytes[i].Length);
                index += 1 + ipBytes[i].Length;
            }

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += 8;

            bytes[index] = (byte)groupidBytes.Length;
            index += 1;
            Array.Copy(groupidBytes, 0, bytes, index, groupidBytes.Length);
            index += groupidBytes.Length;

            bytes[index] = (byte)nameBytes.Length;
            index += 1;
            Array.Copy(nameBytes, 0, bytes, index, nameBytes.Length);
            index += nameBytes.Length;


            bytes[index] = localtcpPort[0];
            bytes[index + 1] = localtcpPort[1];
            index += 2;

            bytes[index] = localudpPort[0];
            bytes[index + 1] = localudpPort[1];
            index += 2;

            Array.Copy(clientAccess, 0, bytes, index, clientAccess.Length);
            index += clientAccess.Length;

            return bytes;

        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            byte ipLength = span[index];
            index += 1;
            LocalIps = new IPAddress[ipLength];
            for (byte i = 0; i < ipLength; i++)
            {
                LocalIps[i] = new IPAddress(span.Slice(index + 1, span[index]));
                index += 1 + span[index];
            }

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;

            GroupId = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Name = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            LocalTcpPort = span.Slice(index, 2).ToUInt16();
            index += 2;
            LocalUdpPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            ClientAccess = span.Slice(index, 4).ToUInt32();
            index += 4;
        }
    }

    /// <summary>
    /// 客户端注册服务器返回的数据
    /// </summary>
    public class RegisterResultInfo
    {
        public RegisterResultInfo() { }

        public RegisterResultInfoCodes Code { get; set; } = RegisterResultInfoCodes.OK;

        /// <summary>
        /// 服务器是否支持中继
        /// </summary>
        public bool Relay { get; set; } = false;

        public int UdpPort { get; set; } = 0;
        public int TcpPort { get; set; } = 0;

        /// <summary>
        /// 连接id
        /// </summary>
        public ulong Id { get; set; } = 0;
        /// <summary>
        /// 连接ip
        /// </summary>
        public IPAddress Ip { get; set; } = IPAddress.Any;
        /// <summary>
        /// 连接分组
        /// </summary>
        public string GroupId { get; set; } = string.Empty;

        public byte[] ToBytes()
        {
            var udpPortBytes = UdpPort.ToBytes();
            var tcpPortBytes = TcpPort.ToBytes();
            var idBytes = Id.ToBytes();
            var ipBytes = Ip.GetAddressBytes();
            var groupIdBytes = GroupId.ToBytes();

            var bytes = new byte[
                1 //Code
                + 1//Relay
                + 2 + 2 //port
                + 8 //id
                + 1 + ipBytes.Length
                + groupIdBytes.Length];

            int index = 0;
            bytes[index] = (byte)Code;
            index += 1;

            bytes[index] = (byte)(Relay ? 1 : 0);
            index += 1;

            bytes[index] = udpPortBytes[0];
            bytes[index + 1] = udpPortBytes[1];
            index += 2;

            bytes[index] = tcpPortBytes[0];
            bytes[index + 1] = tcpPortBytes[1];
            index += 2;

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += 8;

            bytes[index] = (byte)ipBytes.Length;
            Array.Copy(ipBytes, 0, bytes, index + 1, ipBytes.Length);
            index += 1 + ipBytes.Length;

            Array.Copy(groupIdBytes, 0, bytes, index, groupIdBytes.Length);
            index += groupIdBytes.Length;

            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;

            int index = 0;
            Code = (RegisterResultInfoCodes)span[index];
            index += 1;

            Relay = span[index] != 0;
            index += 1;

            UdpPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            TcpPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;


            Ip = new IPAddress(span.Slice(index + 1, span[index]));
            index += 1 + span[index];

            GroupId = span.Slice(index).GetString();
        }

        [Flags]
        public enum RegisterResultInfoCodes : byte
        {
            [Description("成功")]
            OK = 1,
            [Description("存在同名")]
            SAME_NAMES = 2,
            [Description("验证未通过")]
            VERIFY = 4,
            [Description("key验证未通过")]
            KEY_VERIFY = 8,
            [Description("出错")]
            UNKNOW = 16
        }
    }


    public class TunnelRegisterInfo
    {
        public TunnelRegisterInfo() { }

        public ulong TunnelName { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public int Port { get; set; } = 0;

        public byte[] ToBytes()
        {
            var bytes = new byte[12];

            var tunnelNameBytes = TunnelName.ToBytes();
            var localPortBytes = LocalPort.ToBytes();
            var portBytes = Port.ToBytes();

            int index = 0;
            Array.Copy(tunnelNameBytes, 0, bytes, 0, tunnelNameBytes.Length);
            index += 8;

            bytes[index] = localPortBytes[0];
            bytes[index + 1] = localPortBytes[1];
            index += 2;

            bytes[index] = portBytes[0];
            bytes[index + 1] = portBytes[1];

            return bytes;
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            int index = 0;
            TunnelName = data.Span.Slice(index, 8).ToUInt64();
            index += 8;

            LocalPort = data.Span.Slice(index, 2).ToUInt16();
            index += 2;

            Port = data.Span.Slice(index, 2).ToUInt16();
        }
    }

    public enum TunnelDefaults : ulong
    {
        MIN = 0,
        UDP = 1,
        TCP = 2,
        MAX = 2,
    }

    [Flags, MessengerIdEnum]
    public enum RegisterMessengerIds: ushort
    {
        Min = 0,
        SignIn = 0,
        Notify = 1,
        SignOut = 2,

        Max = 100,
    }
}
