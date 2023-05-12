using common.libs.extends;
using System;
using System.Net;

namespace common.server.model
{
    /// <summary>
    /// 客户端列表
    /// </summary>
    public sealed class ClientsInfo
    {
        public ClientsInfo() { }

        /// <summary>
        /// 客户端列表
        /// </summary>
        public ClientsClientInfo[] Clients { get; set; } = Array.Empty<ClientsClientInfo>();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            int length = 0, dataLength = Clients.Length;
            byte[][] dataBytes = new byte[dataLength][];
            for (int i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] = Clients[i].ToBytes();
                length += dataBytes[i].Length;

            }

            int index = 0;
            length += 4;

            var bytes = new byte[length];

            dataLength.ToBytes(bytes);
            index += 4;

            for (int i = 0; i < dataBytes.Length; i++)
            {
                Array.Copy(dataBytes[i], 0, bytes, index, dataBytes[i].Length);
                index += dataBytes[i].Length;
            }
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

            int length = span.Slice(index, 4).ToInt32();
            index += 4;

            Clients = new ClientsClientInfo[length];
            for (int i = 0; i < length; i++)
            {
                Clients[i] = new ClientsClientInfo();
                int tempIndex = Clients[i].DeBytes(data.Slice(index));
                index += tempIndex;
            }
        }

    }

    /// <summary>
    /// 客户端
    /// </summary>
    public sealed class ClientsClientInfo
    {
        /// <summary>
        /// id
        /// </summary>
        public ulong ConnectionId { get; set; }
        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 权限
        /// </summary>
        public uint ClientAccess { get; set; }
        public uint UserAccess { get; set; }

        /// <summary>
        /// 连接对象
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }

        public byte[] ToBytes()
        {
            var nameBytes = Name.GetUTF16Bytes();

            var bytes = new byte[
                4
                + 4
                + 8
                + 1 + 1 + nameBytes.Length
                ];
            var memory = bytes.AsMemory();

            int index = 0;

            ClientAccess.ToBytes(memory.Slice(index));
            index += 4;
            UserAccess.ToBytes(memory.Slice(index));
            index += 4;

            ConnectionId.ToBytes(memory.Slice(index));
            index += 8;

            bytes[index] = (byte)nameBytes.Length;
            index += 1;
            bytes[index] = (byte)Name.Length;
            index += 1;
            nameBytes.CopyTo(memory.Slice(index).Span);
            index += nameBytes.Length;

            return bytes;
        }

        public int DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            ClientAccess = span.Slice(index, 4).ToUInt32();
            index += 4;
            UserAccess = span.Slice(index, 4).ToUInt32();
            index += 4;

            ConnectionId = span.Slice(index, 8).ToUInt64();
            index += 8;

            Name = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 1 + 1 + span[index];

            return index;
        }
    }


    /// <summary>
    /// 客户端相关的消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum ClientsMessengerIds : ushort
    {
        /// <summary>
        /// 
        /// </summary>
        Min = 100,
        /// <summary>
        /// 获取id
        /// </summary>
        IP = 101,
        /// <summary>
        /// 获取端口
        /// </summary>
        Port = 102,
        /// <summary>
        /// 添加通道
        /// </summary>
        AddTunnel = 103,
        /// <summary>
        /// 删除通道
        /// </summary>
        RemoveTunnel = 104,
        /// <summary>
        /// 通知
        /// </summary>
        Notify = 105,
        Exit = 106,
        /// <summary>
        /// 
        /// </summary>
        Max = 199,
    }
}
