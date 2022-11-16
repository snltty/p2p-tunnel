using common.libs;
using common.libs.extends;
using System;
using System.Buffers;
using System.ComponentModel;

namespace common.server.model
{
    public class MessageRequestWrap
    {
        /// <summary>
        /// 用来读取数据，发送数据用下一层的FromConnection，来源连接，在中继时，数据来自服务器，但是真实来源是别的客户端，所以不能直接用这个来发送回复的数据
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 超时时间，发送待回复时设置
        /// </summary>
        public int Timeout { get; set; } = 15000;

        /// <summary>
        /// 目标路径
        /// </summary>
        public ushort MessengerId { get; set; } = 0;

        /// <summary>
        /// 每条数据都有个id，【只发发数据的话，不用填这里】
        /// </summary>
        public uint RequestId = 0;

        /// <summary>
        /// 中继数据时，写明消息给谁，【只发发数据的话，不用填这里】
        /// </summary>
        public ulong[] RelayId { get; set; } = Helper.EmptyUlongArray;

        public Memory<byte> RelayIds { get; private set; } = Helper.EmptyArray;
        public byte RelayIdLength { get; private set; }
        public byte RelayIdIndex { get; private set; }
        public bool Relay { get; set; }
        public bool Reply { get; set; }

        /// <summary>
        /// 数据荷载
        /// </summary>
        public Memory<byte> Payload { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(ServerType type, out int length)
        {
            byte[] requestIdByte = RequestId.ToBytes();
            byte[] messengerIdByte = MessengerId.ToBytes();

            int index = 0;

            length = 4
                + 1 //Relay + Reply + type
                + requestIdByte.Length
                + messengerIdByte.Length
                + Payload.Length;
            if (RelayId.Length > 0)
            {
                length += RelayId.Length * 8 + 2;
                if (Reply == false)
                {
                    length -= 8 * 2;
                }
            }

            byte[] res = ArrayPool<byte>.Shared.Rent(length);
            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (length - 4).ToBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
            }
            index += 4;

            res[index] = (byte)MessageTypes.REQUEST;
            if (Relay == true)
            {
                res[index] = (byte)(res[index] | 0x80);
            }
            if (Reply == true)
            {
                res[index] = (byte)(res[index] | 0x40);
            }
            index += 1;

            if (Relay)
            {
                res[index] = (byte)(Reply ? 2 : 0);

                index += 1;
                res[index] = (byte)(RelayId.Length);
                index += 1;

                int i = (Reply ? 0 : 2);
                for (; i < RelayId.Length; i++)
                {
                    byte[] relayidByte = RelayId[i].ToBytes();
                    Array.Copy(relayidByte, 0, res, index, relayidByte.Length);
                    index += relayidByte.Length;
                }
            }
            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            messengerIdByte.CopyTo(res.AsMemory(index, messengerIdByte.Length));
            index += messengerIdByte.Length;

            Payload.CopyTo(res.AsMemory(index, Payload.Length));
            index += Payload.Length;

            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            Relay = (span[index] & 0x80) == 0x80;
            Reply = (span[index] & 0x40) == 0x40;
            index += 1;

            if (Relay)
            {
                RelayIdIndex = span[index];
                index += 1;
                RelayIdLength = span[index];
                index += 1;

                RelayIds = memory.Slice(index, RelayIdLength * 8);
            }
            else
            {
                RelayIds = Helper.EmptyArray;
                RelayIdIndex = 0;
                RelayIdLength = 0;
            }

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            MessengerId = span.Slice(index, 2).ToUInt16();
            index += 2;

            Payload = memory.Slice(index, memory.Length - index);
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    public class MessageResponseWrap
    {
        public IConnection Connection { get; set; }
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
        public uint RequestId { get; set; } = 0;
        public Memory<byte> RelayIds { get; set; } = Helper.EmptyArray;
        public int RelayIdIndex { get; private set; }
        public byte RelayIdLength { get; private set; }
        public ReadOnlyMemory<byte> Payload { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(ServerType type, out int length)
        {
            length = 4
                + 1 //type
                + 1 //relayid length
                + 1 //code
                + 4 //requestid
                + Payload.Length;
            if (RelayIds.Length > 0)
            {
                length += RelayIds.Length - 8 * 2;
            }

            byte[] res = ArrayPool<byte>.Shared.Rent(length);

            int index = 0;
            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (length - 4).ToBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
            }
            index += 4;

            res[index] = (byte)MessageTypes.RESPONSE;
            index += 1;

            res[index] = 0;
            if (RelayIds.Length > 0)
            {
                res[index] = (byte)(RelayIds.Length - 2);
            }
            index += 1;

            if (RelayIds.Length > 0)
            {
                int len = RelayIds.Length / 8;
                for (int i = 0; i < len - 2; i++)
                {
                    RelayIds.Slice((len - 3 - i) * 8, 8).CopyTo(res.AsMemory(index, 8));
                    index += 8;
                }
            }

            res[index] = (byte)Code;
            index += 1;

            byte[] requestIdByte = RequestId.ToBytes();
            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

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
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 1;

            RelayIdLength = span[index];
            index += 1;
            if (RelayIdLength > 0)
            {
                RelayIdIndex = index;

                RelayIds = memory.Slice(index, RelayIdLength * 8);
            }
            else
            {
                RelayIds = Helper.EmptyArray;
            }


            Code = (MessageResponeCodes)span[index];
            index += 1;

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            if (memory.Length - index > 0)
            {
                Payload = memory.Slice(index, memory.Length - index);
            }
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        public void Reset()
        {
            Payload = Helper.EmptyArray;
            Payload = Helper.EmptyArray;
        }
    }

    [Flags]
    public enum MessageResponeCodes : byte
    {
        [Description("成功")]
        OK = 0,
        [Description("网络未连接")]
        NOT_CONNECT = 1,
        [Description("网络资源未找到")]
        NOT_FOUND = 2,
        [Description("网络超时")]
        TIMEOUT = 3,
        [Description("程序错误")]
        ERROR = 4,
    }

    [Flags]
    public enum MessageTypes : byte
    {
        [Description("请求")]
        REQUEST = 0,
        [Description("回复")]
        RESPONSE = 1
    }

}
