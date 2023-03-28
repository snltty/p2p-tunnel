using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Text;

namespace client.realize.messengers.crypto
{
    /// <summary>
    /// 加密
    /// </summary>
    [MessengerIdRange((ushort)CryptoMessengerIds.Min, (ushort)CryptoMessengerIds.Max)]
    public sealed class CryptoMessenger : IMessenger
    {
        private readonly IAsymmetricCrypto asymmetricCrypto;
        private readonly ICryptoFactory cryptoFactory;
        private readonly Config config;
        public CryptoMessenger(IAsymmetricCrypto asymmetricCrypto, ICryptoFactory cryptoFactory, Config config)
        {
            this.asymmetricCrypto = asymmetricCrypto;
            this.cryptoFactory = cryptoFactory;
            this.config = config;
        }

        /// <summary>
        /// 获取key
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CryptoMessengerIds.Key)]
        public void Key(IConnection connection)
        {
            string publicKey = asymmetricCrypto.Key.PublicKey;
            connection.WriteUTF8(publicKey);
        }

        /// <summary>
        /// 设置密码
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CryptoMessengerIds.Set)]
        public void Set(IConnection connection)
        {
            string password;
            if (connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                var memory = asymmetricCrypto.Decode(connection.ReceiveRequestWrap.Payload);
                password = memory.GetUTF8String();
            }
            else
            {
                password = config.Client.EncodePassword;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                connection.Write(Helper.FalseArray);
                return;
            }
            ISymmetricCrypto encoder = cryptoFactory.CreateSymmetric(password);
            connection.FromConnection.EncodeEnable(encoder);

            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 清除
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CryptoMessengerIds.Clear)]
        public void Clear(IConnection connection)
        {
            connection.FromConnection.EncodeDisable();
            connection.Write(Helper.TrueArray);
        }

        /// <summary>
        /// 测试一下
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CryptoMessengerIds.Test)]
        public void Test(IConnection connection)
        {
            Console.WriteLine($"{connection.ServerType},encoder test : {Encoding.UTF8.GetString(connection.FromConnection.Crypto.Decode(connection.ReceiveRequestWrap.Payload).Span)}");

            connection.Write(Helper.TrueArray);
        }
    }
}
