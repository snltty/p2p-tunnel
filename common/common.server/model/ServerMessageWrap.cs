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
        public string Path
        {
            set
            {
                MemoryPath = value.ToLower().ToBytes();
            }
        }
        /// <summary>
        /// 目标路径，【只发发数据的话，不用填这里】
        /// </summary>
        public Memory<byte> MemoryPath { get; set; } = Memory<byte>.Empty;

        /// <summary>
        /// 每条数据都有个id，【只发发数据的话，不用填这里】
        /// </summary>
        public ulong RequestId { get; set; } = 0;

        /// <summary>
        /// 服务器给客户端发送数据时，可以写1，表示是中继数据，【只发发数据的话，不用填这里】
        /// </summary>
        public byte Relay { get; set; } = 0;
        /// <summary>
        /// 中继数据时，写明是谁发的中继数据，以便目标客户端回复给来源客户端，【只发发数据的话，不用填这里】
        /// </summary>
        public ulong RelayId { get; set; } = 0;
        /// <summary>
        /// 【只发发数据的话，不用填这里】
        /// </summary>
        public Memory<byte> OriginPath { get; set; } = Memory<byte>.Empty;

        /// <summary>
        /// 数据荷载
        /// </summary>
        public Memory<byte> Payload { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(ServerType type, out int length, bool pool = false)
        {
            byte[] requestIdByte = RequestId.ToBytes();
            int index = 0;

            length = (type == ServerType.TCP ? 4 : 0)
                + 1
                + 1
                + requestIdByte.Length
                + 1 + MemoryPath.Length
                + Payload.Length;
            if (Relay == 1)
            {
                length += 8;
                length += 1 + OriginPath.Length;
            }

            byte[] res = pool ? ArrayPool<byte>.Shared.Rent(length) : new byte[length];
            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (length - 4).ToBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = (byte)MessageTypes.REQUEST;
            index += 1;

            res[index] = Relay;
            index += 1;
            if (Relay == 1)
            {
                byte[] delayidByte = RelayId.ToBytes();
                Array.Copy(delayidByte, 0, res, index, delayidByte.Length);
                index += delayidByte.Length;

                res[index] = (byte)OriginPath.Length;
                index += 1;
                OriginPath.CopyTo(res.AsMemory(index, OriginPath.Length));
                index += OriginPath.Length;
            }

            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            res[index] = (byte)MemoryPath.Length;
            index += 1;
            MemoryPath.CopyTo(res.AsMemory(index, MemoryPath.Length));
            index += MemoryPath.Length;

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
            int index = 1;

            Relay = span[index];
            index += 1;

            if (Relay == 1)
            {
                RelayId = span.Slice(index).ToUInt64();
                index += 8;

                byte originPathLength = span[index];
                index += 1;
                OriginPath = memory.Slice(index, originPathLength);
                index += originPathLength;
            }

            RequestId = span.Slice(index).ToUInt64();
            index += 8;

            int pathLength = span[index];
            index += 1;

            MemoryPath = memory.Slice(index, pathLength);
            index += pathLength;

            Payload = memory.Slice(index, memory.Length - index);
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        public void Reset()
        {
            MemoryPath = Memory<byte>.Empty;
            Payload = Helper.EmptyArray;
            Payload = Helper.EmptyArray;
        }
    }
    public class MessageResponseWrap
    {
        public IConnection Connection { get; set; }
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
        public ulong RequestId { get; set; } = 0;
        public ReadOnlyMemory<byte> Payload { get; set; } = Helper.EmptyArray;

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public (byte[] data, int length) ToArray(ServerType type, bool pool = false)
        {
            int length = (type == ServerType.TCP ? 4 : 0)
                + 1
                + 1
                + 8
                + Payload.Length;

            byte[] res = pool ? ArrayPool<byte>.Shared.Rent(length) : new byte[length];

            int index = 0;
            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (length - 4).ToBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = (byte)MessageTypes.RESPONSE;
            index += 1;

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
            return (res, length);
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 1;

            Code = (MessageResponeCodes)span[index];
            index += 1;

            RequestId = span.Slice(index).ToUInt64();
            index += 8;

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
