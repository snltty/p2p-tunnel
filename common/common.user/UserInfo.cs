using common.libs.extends;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace common.user
{
    public sealed class UserInfo
    {
        public ulong ID { get; set; }
        public string Account { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// 权限，0无任何权限
        /// </summary>
        public uint Access { get; set; }
        /// <summary>
        /// 限制登录数，-1无限制
        /// </summary>
        public int SignLimit { get; set; } = -1;
        private uint signCount = 0;
        public uint SignCount
        {
            get
            {
                if (signCount < Connections.Count)
                {
                    signCount = (uint)Connections.Count;
                }
                return signCount;
            }
            set
            {
                signCount = value;
            }
        }

        [JsonIgnore]
        public ConcurrentDictionary<ulong, IConnection> Connections { get; set; } = new ConcurrentDictionary<ulong, IConnection>();
        /// <summary>
        /// 限制流量 -1 无限制
        /// </summary>
        public long NetFlow { get; set; } = -1;
        public ulong SentBytes { get; set; }

        /// <summary>
        /// 账号添加时间
        /// </summary>
        public DateTime AddTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 账号结束时间
        /// </summary>
        public DateTime EndTime { get; set; } = DateTime.Now;


        public const byte SortID = 0b0000_0001;
        public const byte SortAddTime = 0b0000_0010;
        public const byte SortEndTime = 0b0000_0100;
        public const byte SortNetFlow = 0b0000_1000;
        public const byte SortSignLimit = 0b0001_0000;
        public const byte SortAsc = 0b0000_00000;
        public const byte SortDesc = 0b0000_0001;
    }

    public sealed class UserInfoPageModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Account { get; set; } = string.Empty;
        public byte Sort { get; set; }

        public byte[] ToBytes()
        {
            int index = 0;
            var accountBytes = Account.GetUTF16Bytes();
            var bytes = new byte[
                8  //Page+PageSize
                + 1 //Sort
                + 2 + accountBytes.Length];
            var span = bytes.AsSpan();

            Page.ToBytes(bytes);
            index += 4;

            PageSize.ToBytes(bytes.AsMemory(index));
            index += 4;

            span[index] = Sort;
            index += 1;

            span[index] = (byte)accountBytes.Length;
            index += 1;
            span[index] = (byte)Account.Length;
            index += 1;
            accountBytes.CopyTo(span.Slice(index));

            return bytes;

        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            Page = span.Slice(index, 4).ToInt32();
            index += 4;

            PageSize = span.Slice(index, 4).ToInt32();
            index += 4;

            Sort = span[index];
            index += 1;

            Account = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 2 + span[index];
        }
    }

    public sealed class UserPasswordInfo
    {
        public ulong ID { get; set; }
        public string Password { get; set; }

        public byte[] ToBytes()
        {
            int index = 0;
            var passwordBytes = Password.GetUTF16Bytes();
            var bytes = new byte[
                8  //ID
                + 2 + passwordBytes.Length];
            var span = bytes.AsSpan();

            ID.ToBytes(bytes);
            index += 8;

            span[index] = (byte)passwordBytes.Length;
            index += 1;
            span[index] = (byte)Password.Length;
            index += 1;
            passwordBytes.CopyTo(span.Slice(index));

            return bytes;

        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            ID = span.Slice(index, 8).ToUInt64();
            index += 8;

            Password = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
            index += 2 + span[index];
        }
    }

    public sealed class UserSignInfo
    {
        public ulong UserId { get; set; }
        public ulong ConnectionId { get; set; }

        public byte[] ToBytes()
        {
            int index = 0;
            var bytes = new byte[8 * 2];
            var span = bytes.AsSpan();

            UserId.ToBytes(bytes);
            index += 8;

            ConnectionId.ToBytes(bytes.AsMemory(index));
            index += 8;

            return bytes;

        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int index = 0;

            UserId = span.Slice(index, 8).ToUInt64();
            index += 8;

            ConnectionId = span.Slice(index, 8).ToUInt64();
            index += 8;
        }
    }

    public sealed class UserInfoPageResultModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public List<UserInfo> Data { get; set; }
    }

    /// <summary>
    /// 权限相关的消息id
    /// </summary>
    [Flags, MessengerIdEnum]
    public enum UsersMessengerIds : ushort
    {
        Min = 1000,
        /// <summary>
        /// 分页
        /// </summary>
        Page = 1001,
        /// <summary>
        /// 添加修改
        /// </summary>
        Add = 1002,
        /// <summary>
        /// 更新密码
        /// </summary>
        Password = 1003,
        PasswordSelf = 1004,
        /// <summary>
        /// 删除
        /// </summary>
        Remove = 1005,
        /// <summary>
        /// 信息
        /// </summary>
        Info = 1006,

        /// <summary>
        /// 获取配置
        /// </summary>
        GetSetting = 1007,
        /// <summary>
        /// 设置配置
        /// </summary>
        Setting = 1008,

        /// <summary>
        /// 验证登入
        /// </summary>
        SignIn = 1009,

        Max = 1099,
    }
}
