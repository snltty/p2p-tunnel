using System;

namespace common.socks5
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISocks5ServerHandler
    {
        /// <summary>
        /// 
        /// </summary>
        void InputData(Socks5Info data);
    }

    public interface ISocks5AuthValidator
    {
        /// <summary>
        /// 返回你需要的认证方式
        /// </summary>
        /// <param name="authTypes"></param>
        /// <returns></returns>
        Socks5EnumAuthType GetAuthType(Socks5EnumAuthType[] authTypes);

        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Socks5EnumAuthState Validate(Memory<byte> data, Socks5EnumAuthType authType);
    }

    public class Socks5AuthValidator : ISocks5AuthValidator
    {
        public Socks5EnumAuthType GetAuthType(Socks5EnumAuthType[] authTypes)
        {
            return Socks5EnumAuthType.NoAuth;
        }
        public Socks5EnumAuthState Validate(Memory<byte> data, Socks5EnumAuthType authType)
        {
            return Socks5EnumAuthState.Success;
        }


    }
}
