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
    public  enum PunchHoleTypes : byte
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
    public sealed class PunchHoleStep1Info : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; }
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;
        public byte Step { get; set; }
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
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
    public sealed class PunchHoleStep2Info : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; }
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;
        public byte Step { get; set; }
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    public sealed class PunchHoleStep2FailInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; }

        public PunchForwardTypes ForwardType { get; set; }

        public byte Step { get; set; }
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    public sealed class PunchHoleStep3Info : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; }
        public PunchForwardTypes ForwardType { get; set; }
        public byte Step { get; set; }
        public byte[] ToBytes()
        {
            var bytes = new byte[3];
            bytes[0] = (byte)PunchType;
            bytes[1] = (byte)ForwardType;
            bytes[2] = Step;
            return bytes;
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }

    }
    public sealed class PunchHoleStep4Info : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; }
        public PunchForwardTypes ForwardType { get; set; }
        public byte Step { get; set; }
        public byte[] ToBytes()
        {
            var bytes = new byte[3 + 8];
            bytes[0] = (byte)PunchType;
            bytes[1] = (byte)ForwardType;
            bytes[2] = Step;

            return bytes;
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }

    }
    public sealed class PunchHoleReverseInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.REVERSE;
        public PunchForwardTypes ForwardType { get; set; } 
        public byte Step { get; set; } 

        /// <summary>
        /// 4个位，表示两边的tcp udp打洞情况
        /// </summary>
        public byte Value { get; set; }

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
                Value
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
            Value = span[3];
        }
    }

    public sealed class PunchHoleOfflineInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; private set; } = PunchHoleTypes.OFFLINE;

        public PunchForwardTypes ForwardType { get; private set; }
        public byte Step { get; set; }
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

    public sealed class PunchHoleResetInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; private set; } = PunchHoleTypes.RESET;

        public PunchForwardTypes ForwardType { get; private set; }

        public byte Step { get; set; }

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

}
