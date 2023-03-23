using common.libs.extends;
using common.server;
using System;
using System.Collections.Generic;

namespace common.server.model
{
    public class UserInfo
    {
        public ulong ID { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// 权限，0无任何权限
        /// </summary>
        public uint Access { get; set; }
        /// <summary>
        /// 限制登录数，0无限制
        /// </summary>
        public uint SignLimit { get; set; }
        /// <summary>
        /// 限制流量 -1 无限制
        /// </summary>
        public long NetFlow { get; set; } = -1;
        /// <summary>
        /// 账号结束时间
        /// </summary>
        public DateTime EndTime { get; set; }
    }

    public class UserInfoPageModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }

        public byte[] ToBytes()
        {
            int index = 0;
            var bytes = new byte[8];

            Page.ToBytes(bytes);
            index += 4;

            PageSize.ToBytes(bytes);
            index += 4;
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
        }
    }
    public class UserInfoPageResultModel
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
        Min = 1200,
        /// <summary>
        /// 分页
        /// </summary>
        Page = 1201,
        /// <summary>
        /// 添加修改
        /// </summary>
        Add = 1202,
        /// <summary>
        /// 删除
        /// </summary>
        Remove = 1203,
        Max = 1299,
    }
}
