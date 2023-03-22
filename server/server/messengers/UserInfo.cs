using common.server.model;
using System;

namespace server.messengers
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
}
