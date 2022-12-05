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
        /// <summary>
        /// 
        /// </summary>
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
            var lengthBytes = dataLength.ToBytes();
            length += lengthBytes.Length;

            var bytes = new byte[length];

            Array.Copy(lengthBytes, 0, bytes, index, lengthBytes.Length);
            index += lengthBytes.Length;

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
        public ulong Id { get; set; } = 0;
        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 权限
        /// </summary>
        public uint Access { get; set; } = 0;

        /// <summary>
        /// 连接对象
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var idBytes = Id.ToBytes();
            var nameBytes = Name.ToBytes();
            var clientAccessBytes = Access.ToBytes();

            var bytes = new byte[
                4
                + 8
                + 1 + nameBytes.Length
                ];

            int index = 0;

            Array.Copy(clientAccessBytes, 0, bytes, index, clientAccessBytes.Length);
            index += 4;

            Array.Copy(idBytes, 0, bytes, index, idBytes.Length);
            index += 8;

            bytes[index] = (byte)nameBytes.Length;
            Array.Copy(nameBytes, 0, bytes, index + 1, nameBytes.Length);
            index += 1 + nameBytes.Length;

            return bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            Access = span.Slice(index, 4).ToUInt32();
            index += 4;

            Id = span.Slice(index, 8).ToUInt64();
            index += 8;

            Name = span.Slice(index + 1, span[index]).GetString();
            index += 1 + span[index];

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
        /// <summary>
        /// 
        /// </summary>
        Max = 199,
    }
}
