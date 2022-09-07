using common.libs;
using common.libs.extends;
using System;
using System.ComponentModel;
using System.Net;

namespace common.server.model
{
    /// <summary>
    /// 打洞数据交换
    /// </summary>
    public class PunchHoleParamsInfo
    {
        public PunchHoleParamsInfo() { }
        /// <summary>
        /// 数据交换分两种，一种是a让服务器把a的公网数据发给b，另一种是，a把一些数据通过服务器原样交给b
        /// </summary>
        public PunchForwardTypes PunchForwardType { get; set; } = PunchForwardTypes.NOTIFY;
        /// <summary>
        /// 打洞步骤，这个数据是第几步
        /// </summary>
        public byte PunchStep { get; set; } = 0;
        /// <summary>
        /// 打洞类别，tcp udp 或者其它
        /// </summary>
        public byte PunchType { get; set; } = 0;
        /// <summary>
        /// 猜测的端口
        /// </summary>
        public int GuessPort { get; set; } = 0;
        /// <summary>
        /// 来自谁
        /// </summary>
        public ulong FromId { get; set; } = 0;
        /// <summary>
        /// 给谁
        /// </summary>
        public ulong ToId { get; set; } = 0;
        /// <summary>
        /// 通道名，可能会有多个通道
        /// </summary>
        public string TunnelName { get; set; } = string.Empty;

        /// <summary>
        /// 携带的数
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public ReadOnlyMemory<byte> Data { get; set; } = Helper.EmptyArray;
        public byte[] ToBytes()
        {
            var guessPortBytes = GuessPort.ToBytes();
            var fromidBytes = FromId.ToBytes();
            var toidBytes = ToId.ToBytes();
            var nameBytes = TunnelName.ToBytes();

            var bytes = new byte[
                1 + 1 + 1
                + 2
                + 8 + 8
                + 1 + nameBytes.Length + Data.Length
                ];
            int index = 0;

            bytes[index] = (byte)PunchForwardType;
            index += 1;
            bytes[index] = PunchStep;
            index += 1;
            bytes[index] = PunchType;
            index += 1;

            bytes[index] = guessPortBytes[0];
            bytes[index + 1] = guessPortBytes[1];
            index += 2;

            Array.Copy(fromidBytes, 0, bytes, index, fromidBytes.Length);
            index += 8;
            Array.Copy(toidBytes, 0, bytes, index, toidBytes.Length);
            index += 8;

            bytes[index] = (byte)nameBytes.Length;
            Array.Copy(nameBytes, 0, bytes, index + 1, nameBytes.Length);
            index += 1 + nameBytes.Length;

            Data.CopyTo(bytes.AsMemory(index));

            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            PunchForwardType = (PunchForwardTypes)span[index];
            index += 1;
            PunchStep = span[index];
            index += 1;
            PunchType = span[index];
            index += 1;

            GuessPort = span.Slice(index, 2).ToUInt16();
            index += 2;

            FromId = span.Slice(index, 8).ToUInt64();
            index += 8;

            ToId = span.Slice(index, 8).ToUInt64();
            index += 8;

            TunnelName = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

            Data = data.Slice(index);

        }
    }

    [Flags]
    public enum PunchForwardTypes : byte
    {
        [Description("通知A的数据给B")]
        NOTIFY,
        [Description("原样转发")]
        FORWARD
    }

    public class PunchHoleNotifyInfo
    {
        public PunchHoleNotifyInfo() { }

        public IPAddress[] LocalIps { get; set; } = Array.Empty<IPAddress>();
        public bool IsDefault { get; set; } = false;
        public IPAddress Ip { get; set; } = IPAddress.Any;
        public int Port { get; set; } = 0;
        public int LocalPort { get; set; } = 0;
        public int GuessPort { get; set; } = 0;

        public byte[] ToBytes()
        {
            int length = 0;

            byte[][] ipsBytes = new byte[LocalIps.Length][];
            for (int i = 0; i < LocalIps.Length; i++)
            {
                ipsBytes[i] = LocalIps[i].GetAddressBytes();
                length += 1 + ipsBytes[i].Length;
            }
            length += 1;

            length += 1; //IsDefault

            var ipBytes = Ip.GetAddressBytes();
            length += 1 + ipBytes.Length;

            var port = Port.ToBytes();
            length += 2;
            var localport = LocalPort.ToBytes();
            length += 2;
            var guessPortBytes = GuessPort.ToBytes();
            length += 2;


            var bytes = new byte[length];
            int index = 0;
            bytes[index] = (byte)ipsBytes.Length;
            index += 1;
            for (int i = 0; i < ipsBytes.Length; i++)
            {
                bytes[index] = (byte)ipsBytes[i].Length;
                Array.Copy(ipsBytes[i], 0, bytes, index + 1, ipsBytes[i].Length);
                index += 1 + ipsBytes[i].Length;
            }

            bytes[index] = (byte)(IsDefault ? 1 : 0);
            index += 1;

            bytes[index] = (byte)ipBytes.Length;
            Array.Copy(ipBytes, 0, bytes, index + 1, ipBytes.Length);
            index += 1 + ipBytes.Length;


            bytes[index] = port[0];
            bytes[index + 1] = port[1];
            index += 2;
            bytes[index] = localport[0];
            bytes[index + 1] = localport[1];
            index += 2;
            bytes[index] = guessPortBytes[0];
            bytes[index + 1] = guessPortBytes[1];
            index += 2;

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

            IsDefault = span[index] == 1 ? true : false;
            index += 1;

            Ip = new IPAddress(span.Slice(index + 1, span[index]));
            index += 1 + span[index];

            Port = span.Slice(index, 2).ToUInt16();
            index += 2;
            LocalPort = span.Slice(index, 2).ToUInt16();
            index += 2;
            GuessPort = span.Slice(index, 2).ToUInt16();
            index += 2;
        }
    }

}
