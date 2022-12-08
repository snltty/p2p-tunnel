using common.libs.extends;
using common.server.model;
using System;
using System.ComponentModel;

namespace client.messengers.punchHole
{
    /// <summary>
    /// 打洞类型
    /// </summary>
    [Flags]
    public enum PunchHoleTypes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("UDP打洞")]
        UDP = 0,
        /// <summary>
        /// 
        /// </summary>
        [Description("IP欺骗打洞")]
        TCP_NUTSSA = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("端口复用打洞")]
        TCP_NUTSSB = 2,
        /// <summary>
        /// 
        /// </summary>
        [Description("反向链接")]
        REVERSE = 4,
        /// <summary>
        /// 
        /// </summary>
        [Description("重启")]
        RESET = 8,
        /// <summary>
        /// 
        /// </summary>
        [Description("中继")]
        RELAY = 16,
        /// <summary>
        /// 
        /// </summary>
        [Description("创建通道")]
        TUNNEL = 32,
        /// <summary>
        /// 
        /// </summary>
        [Description("掉线")]
        OFFLINE = 64,
    }

    /// <summary>
    /// 打洞步骤
    /// </summary>
    public interface IPunchHoleStepInfo
    {
        /// <summary>
        /// 打洞类型
        /// </summary>
        PunchHoleTypes PunchType { get; }
        /// <summary>
        /// 数据通知类型
        /// </summary>
        public PunchForwardTypes ForwardType { get; }
        /// <summary>
        /// 步骤
        /// </summary>
        public byte Step { get; set; }
        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns></returns>
        byte[] ToBytes();
    }

    /// <summary>
    /// 步骤1
    /// </summary>
    public class PunchHoleStep1Info : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;
        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

   /// <summary>
   /// 步骤2
   /// </summary>
    public class PunchHoleStep2Info : IPunchHoleStepInfo
    {
       /// <summary>
       /// 
       /// </summary>
        public PunchHoleTypes PunchType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleStep2FailInfo : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleStep3Info : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[3];
            bytes[0] = (byte)PunchType;
            bytes[1] = (byte)ForwardType;
            bytes[2] = Step;
            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }

    }
    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleStep4Info : IPunchHoleStepInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[3 + 8];
            bytes[0] = (byte)PunchType;
            bytes[1] = (byte)ForwardType;
            bytes[2] = Step;

            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }

    }
    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleReverseInfo : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.REVERSE;

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; } 

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; } 

        /// <summary>
        /// 4个位，表示两边的tcp udp打洞情况
        /// </summary>
        public byte Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
                Value
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
            Value = span[3];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleOfflineInfo : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; private set; } = PunchHoleTypes.OFFLINE;

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleResetInfo : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; private set; } = PunchHoleTypes.RESET;

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleTunnelInfo : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; private set; } = PunchHoleTypes.TUNNEL;

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ulong TunnelName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[12];

            int index = 0;
            bytes[index] = (byte)PunchType;
            index += 1;
            bytes[index] = (byte)ForwardType;
            index += 1;
            bytes[index] = Step;
            index += 1;

            TunnelName.ToBytes(bytes.AsMemory(index));

            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];

            TunnelName = span.Slice(3, 8).ToUInt64();
        }
    }
}
