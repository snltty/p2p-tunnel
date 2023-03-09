using common.libs;
using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    public sealed class RegisterParamsInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public RegisterParamsInfo() { }

        /// <summary>
        /// 本地ip，loopback 、LAN ip
        /// </summary>
        public IPAddress[] LocalIps { get; set; }
        /// <summary>
        /// 连接id，因为分两次注册，第二次带上第一次的注册后获得的id
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte ShortId { get; set; }
        /// <summary>
        /// 分组
        /// </summary>
        public string GroupId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 本机tcp端口
        /// </summary>
        public ushort LocalTcpPort { get; set; }

        /// <summary>
        /// 客户端自定义的权限列表
        /// </summary>
        public uint ClientAccess { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            int length = 1;

            for (int i = 0; i < LocalIps.Length; i++)
            {
                length += 1 + LocalIps[i].Length();
            }

            var groupidBytes = GroupId.GetUTF16Bytes();
            var nameBytes = Name.GetUTF16Bytes();
            length +=
                8 //Id
                + 1 //ShortId
                + 1 + 1 + groupidBytes.Length
                + 1 + 1 + nameBytes.Length
                + 2 //LocalTcpPort
                + 4; //ClientAccess

            var bytes = new byte[length];
            var memory = bytes.AsMemory();
            int index = 0;

            bytes[index] = (byte)LocalIps.Length;
            index += 1;
            for (int i = 0; i < LocalIps.Length; i++)
            {
                LocalIps[i].TryWriteBytes(memory.Span.Slice(index + 1), out int ll);
                bytes[index] = (byte)ll;
                index += 1 + ll;
            }

            Id.ToBytes(memory.Slice(index));
            index += 8;
            bytes[index] = ShortId;
            index += 1;

            bytes[index] = (byte)groupidBytes.Length;
            index += 1;
            bytes[index] = (byte)GroupId.Length;
            index += 1;
            groupidBytes.CopyTo(memory.Span.Slice(index));
            index += groupidBytes.Length;

            bytes[index] = (byte)nameBytes.Length;
            index += 1;
            bytes[index] = (byte)Name.Length;
            index += 1;
            nameBytes.CopyTo(memory.Span.Slice(index));
            index += nameBytes.Length;


            LocalTcpPort.ToBytes(memory.Slice(index));
            index += 2;

            ClientAccess.ToBytes(memory.Slice(index));
            index += 4;

            return bytes;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
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
            ShortId = span[index];
            index += 1;

            GroupId = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 2 + span[index];

            Name = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 2 + span[index];

            LocalTcpPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            ClientAccess = span.Slice(index, 4).ToUInt32();
            index += 4;
        }
    }

    /// <summary>
    /// 客户端注册服务器返回的数据
    /// </summary>
    public sealed class RegisterResultInfo
    {
        public RegisterResultInfo() { }

        public RegisterResultInfoCodes Code { get; set; }

        /// <summary>
        /// 服务器是否支持中继
        /// </summary>
        public bool Relay { get; set; }

        /// <summary>
        /// 连接id
        /// </summary>
        public ulong Id { get; set; }
        public byte ShortId { get; set; }
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
            var groupIdBytes = GroupId.GetUTF16Bytes();

            var bytes = new byte[
                1 //Code
                + 1//Relay
                + 9 //id
                + 1 + Ip.Length()
                + 1 + groupIdBytes.Length];

            var memory = bytes.AsMemory();

            int index = 0;
            bytes[index] = (byte)Code;
            index += 1;

            bytes[index] = (byte)(Relay ? 1 : 0);
            index += 1;

            Id.ToBytes(memory.Slice(index));
            index += 8;
            bytes[index] = ShortId;
            index += 1;

            if (Ip != null)
            {
                Ip.TryWriteBytes(memory.Span.Slice(index + 1), out int ll);
                bytes[index] = (byte)ll;
                index += 1 + ll;
            }
            else
            {
                bytes[index] = 0;
                index += 1 + 0;
            }

            bytes[index] = (byte)GroupId.Length;
            index += 1;
            groupIdBytes.CopyTo(memory.Span.Slice(index));
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

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;
            ShortId = span[index];
            index += 1;


            if (span[index] == 0)
            {
                Ip = IPAddress.Any;
            }
            else
            {
                Ip = new IPAddress(span.Slice(index + 1, span[index]));
            }
            index += 1 + span[index];


            GroupId = span.Slice(index + 1).GetUTF16String(span[index]);
        }

        /// <summary>
        /// 注册结果类别
        /// </summary>
        [Flags]
        public enum RegisterResultInfoCodes : byte
        {
            /// <summary>
            /// 
            /// </summary>
            [Description("成功")]
            OK = 0,
            /// <summary>
            /// 
            /// </summary>
            [Description("存在同名客户端")]
            SAME_NAMES = 1,
            /// <summary>
            /// 
            /// </summary>
            [Description("存在同名短id")]
            SAME_SHORTID = 2,
            /// <summary>
            /// 
            /// </summary>
            [Description("短id获取失败")]
            ERROR_SHORTID = 4,
            /// <summary>
            /// 
            /// </summary>
            [Description("验证未通过")]
            VERIFY = 8,
            /// <summary>
            /// 
            /// </summary>
            [Description("key验证未通过")]
            KEY_VERIFY = 16,
            /// <summary>
            /// 
            /// </summary>
            [Description("出错")]
            UNKNOW = 32
        }
    }


    /// <summary>
    /// 端口注册
    /// </summary>
    public sealed class TunnelRegisterInfo
    {
        public TunnelRegisterInfo() { }

        public ulong SelfId { get; set; }
        public ulong TargetId { get; set; }
        public ushort LocalPort { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[18];
            var memory = bytes.AsMemory();

            int index = 0;
            SelfId.ToBytes(memory);
            index += 8;
            TargetId.ToBytes(memory);
            index += 8;

            LocalPort.ToBytes(memory.Slice(index));
            index += 2;

            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            int index = 0;
            SelfId = data.Span.Slice(index, 8).ToUInt64();
            index += 8;
            TargetId = data.Span.Slice(index, 8).ToUInt64();
            index += 8;

            LocalPort = data.Span.Slice(index, 2).ToUInt16();
            index += 2;
        }
    }

    /// <summary>
    /// 通道默认值
    /// </summary>
    public enum TunnelDefaults : ulong
    {
        /// <summary>
        /// 使用此值，则生成新的值
        /// </summary>
        MIN = 0,
        /// <summary>
        /// 
        /// </summary>
        UDP = 1,
        /// <summary>
        /// 
        /// </summary>
        TCP = 2,
        /// <summary>
        /// 
        /// </summary>
        MAX = 2,
    }

    /// <summary>
    /// 注册相关的消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum RegisterMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 0,
        /// <summary>
        /// 注册
        /// </summary>
        SignIn = 0,
        /// <summary>
        /// 通知
        /// </summary>
        Notify = 1,
        /// <summary>
        /// 退出
        /// </summary>
        SignOut = 2,
        /// <summary>
        /// 获取配置
        /// </summary>
        GetSetting = 3,
        /// <summary>
        /// 配置
        /// </summary>
        Setting = 4,
        Test = 5,
        /// <summary>
        /// 
        /// </summary>
        Max = 99,
    }
}
