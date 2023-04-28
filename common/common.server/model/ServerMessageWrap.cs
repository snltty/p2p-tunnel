using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace common.server.model
{
    /// <summary>
    /// 请求消息包
    /// |                 Method |      Mean |    Error |    StdDev |    Median | Rank |   Gen0 | Allocated |
    ///|----------------------- |----------:|---------:|----------:|----------:|-----:|-------:|----------:|
    ///|    MemoryPackSerialize | 279.01 ns | 1.927 ns |  5.142 ns | 277.56 ns |    4 | 0.5274 |    1104 B |
    ///|   MessagePackSerialize | 312.54 ns | 5.064 ns | 13.603 ns | 304.24 ns |    5 | 0.5355 |    1120 B |
    ///|                ToArray |  89.05 ns | 0.354 ns |  0.946 ns |  89.02 ns |    2 |      - |         - |
    ///|  MemoryPackDeserialize | 199.63 ns | 2.398 ns |  6.644 ns | 197.79 ns |    3 | 0.5581 |    1168 B |
    ///| MessagePackDeserialize | 531.59 ns | 2.387 ns |  6.454 ns | 530.47 ns |    6 | 0.5579 |    1168 B |
    ///不new
    ///|              FromArray |  14.95 ns | 0.127 ns |  0.350 ns |  14.93 ns |    1 |      - |         - |
    ///每次new
    ///|              FromArray |  24.89 ns | 0.188 ns |  0.504 ns |  24.80 ns |    1 | 0.0344 |      72 B |
    /// </summary>
    public sealed class MessageRequestWrap
    {
        #region 字段
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdLengthPos = 5;
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdIndexPos = RelayIdLengthPos + 1;
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdSize = 8;
        /// <summary>
        /// Relay + Reply + EncodeBit + 00001
        /// </summary>
        public const byte RelayBit = 0b10000000;
        /// <summary>
        /// 
        /// </summary>
        public const byte ReplyBit = 0b01000000;
        /// <summary>
        /// 
        /// </summary>
        public const byte EncodeBit = 0b00100000;
        /// <summary>
        /// 
        /// </summary>
        public const byte TypeBits = 0b00000001;

        /// <summary>
        /// 超时时间，发送待回复时设置
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        public ushort MessengerId { get; set; }
        /// <summary>
        /// 每条数据都有个id，自动设置，无需填写【只发发数据的话，不用填这里】
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 是否等待回复，自动设置，无需填写
        /// </summary>
        public bool Reply { get; internal set; }

        /// <summary>
        /// 是否中继，自动设置，无需填写
        /// </summary>
        public bool Relay { get; set; }
        /// <summary>
        /// 加密，自动设置，无需填写
        /// </summary>
        public bool Encode { get; set; } = true;


        /// <summary>
        /// 中继节点id列表
        /// </summary>
        public Memory<ulong> RelayId { get; set; }
        /// <summary>
        /// 中继节点id列表，读取用，自动设置，无需填写
        /// </summary>
        public Memory<byte> RelayIds { get;private set; }
        /// <summary>
        /// 中继经过节点数量，自动设置，无需填写
        /// </summary>
        public byte RelayIdLength { get; private set; }
        /// <summary>
        /// 中继下，当前所在节点，自动设置，无需填写
        /// </summary>
        public byte RelayIdIndex { get; private set; }

        /// <summary>
        /// 数据荷载
        /// </summary>
        public Memory<byte> Payload { get; set; }
        #endregion

        public IConnection Connection { get; set; }

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(out int length)
        {
            int index = 0;

            length = 4
                + 1 //Relay + Reply + type
                + 4
                + 2
                + Payload.Length;
            if (Relay)
            {
                length += RelayId.Length * RelayIdSize + 2; //length index
            }
            byte[] res = ArrayPool<byte>.Shared.Rent(length);

            ((uint)length - 4).ToBytes(res);
            index += 4;

            res[index] = (byte)MessageTypes.REQUEST;
            if (Relay == true)
            {
                res[index] |= RelayBit;
            }
            if (Reply == true)
            {
                res[index] |= ReplyBit;
            }
            if (Encode == true)
            {
                res[index] |= EncodeBit;
            }
            index += 1;

            if (Relay)
            {
                res[index] = (byte)(RelayId.Length); //length
                index += 1;

                res[index] = 2; //index
                index += 1;

                RelayId.ToBytes(res.AsMemory(index));
                index += RelayId.Length * RelayIdSize;
            }

            RequestId.ToBytes(res.AsMemory(index));
            index += 4;

            MessengerId.ToBytes(res.AsMemory(index));
            index += 2;

            Payload.CopyTo(res.AsMemory(index, Payload.Length));
            index += Payload.Length;

            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="memory"></param>
        public unsafe void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            Relay = (span[index] & RelayBit) == RelayBit;
            Reply = (span[index] & ReplyBit) == ReplyBit;
            Encode = (span[index] & EncodeBit) == EncodeBit;
            index += 1;

            if (Relay)
            {
                RelayIdLength = span[index];
                index += 1;

                RelayIdIndex = span[index];
                index += 1;

                RelayIds = memory.Slice(index, RelayIdLength * RelayIdSize);
                index += RelayIdLength * RelayIdSize;
            }
            else
            {
                RelayIds = Helper.EmptyArray;
                RelayIdIndex = 0;
                RelayIdLength = 0;
            }


            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            MessengerId = span.Slice(index).ToUInt16();
            index += 2;

            Payload = memory.Slice(index, memory.Length - index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

    }

    /// <summary>
    /// 回执消息包
    /// </summary>
    public sealed class MessageResponseWrap
    {
        /// <summary>
        /// 
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public MessageResponeCodes Code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Memory<byte> RelayIds { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte RelayIdLength { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public byte RelayIdIndex { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public Memory<byte> Payload { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Relay { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Encode { get; set; }

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(out int length)
        {
            length = 4
                + 1 //type
                + 1 //code
                + 4 //requestid
                + Payload.Length;

            if (RelayIds.Length > 0)
            {
                RelayIdLength = (byte)(RelayIds.Length / MessageRequestWrap.RelayIdSize);
                length += RelayIds.Length + 2; //length + index
            }

            byte[] res = ArrayPool<byte>.Shared.Rent(length);

            int index = 0;
            ((uint)length - 4).ToBytes(res);
            index += 4;

            res[index] = (byte)MessageTypes.RESPONSE;
            if (Encode)
            {
                res[index] |= MessageRequestWrap.EncodeBit;
            }
            index += 1;

            if (RelayIdLength > 0)
            {
                res[index - 1] |= MessageRequestWrap.RelayBit;
                res[index] = RelayIdLength;
                index += 1;
                res[index] = 2;
                index += 1;

                for (int i = 0; i < RelayIdLength; i++)
                {
                    RelayIds.Slice((RelayIdLength - 1 - i) * MessageRequestWrap.RelayIdSize, MessageRequestWrap.RelayIdSize).CopyTo(res.AsMemory(index, MessageRequestWrap.RelayIdSize));
                    index += MessageRequestWrap.RelayIdSize;
                }
            }

            res[index] = (byte)Code;
            index += 1;

            RequestId.ToBytes(res.AsMemory(index));
            index += 4;

            if (Payload.Length > 0)
            {
                Payload.CopyTo(res.AsMemory(index, Payload.Length));
                index += Payload.Length;
            }
            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="memory"></param>
        public void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 0;

            Relay = (span[index] & MessageRequestWrap.RelayBit) == MessageRequestWrap.RelayBit;
            Encode = (span[index] & MessageRequestWrap.EncodeBit) == MessageRequestWrap.EncodeBit;
            index += 1;

            if (Relay)
            {
                RelayIdLength = span[index];
                index += 1;
                RelayIdIndex = span[index];
                index += 1;

                RelayIds = memory.Slice(index, RelayIdLength * MessageRequestWrap.RelayIdSize);
                index += RelayIdLength * MessageRequestWrap.RelayIdSize;
            }
            else
            {
                RelayIdLength = 0;
                RelayIdIndex = 0;
                RelayIds = Helper.EmptyArray;
            }

            Code = (MessageResponeCodes)span[index];
            index += 1;

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            Payload = memory.Slice(index, memory.Length - index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// 消息状态
    /// </summary>
    [Flags]
    public enum MessageResponeCodes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("成功")]
        OK = 0,
        /// <summary>
        /// 
        /// </summary>
        [Description("网络未连接")]
        NOT_CONNECT = 1,
        /// <summary>
        /// 
        /// </summary>
        [Description("网络资源未找到")]
        NOT_FOUND = 2,
        /// <summary>
        /// 
        /// </summary>
        [Description("网络超时")]
        TIMEOUT = 3,
        /// <summary>
        /// 
        /// </summary>
        [Description("程序错误")]
        ERROR = 4,
    }

    /// <summary>
    /// 消息类别
    /// </summary>
    [Flags]
    public enum MessageTypes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("请求")]
        REQUEST = 0,
        /// <summary>
        /// 
        /// </summary>
        [Description("回复")]
        RESPONSE = 1
    }

}

