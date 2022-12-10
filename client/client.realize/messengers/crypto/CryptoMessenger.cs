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
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly Config config;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asymmetricCrypto"></param>
        /// <param name="cryptoFactory"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="config"></param>
        public CryptoMessenger(IAsymmetricCrypto asymmetricCrypto, ICryptoFactory cryptoFactory, IClientInfoCaching clientInfoCaching, Config config)
        {
            this.asymmetricCrypto = asymmetricCrypto;
            this.cryptoFactory = cryptoFactory;
            this.clientInfoCaching = clientInfoCaching;
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
            connection.WriteUTF8(asymmetricCrypto.Key.PublicKey);
        }

        /// <summary>
        /// 设置密码
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CryptoMessengerIds.Set)]
        public byte[] Set(IConnection connection)
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
                return Helper.FalseArray;
            }

            ISymmetricCrypto encoder = cryptoFactory.CreateSymmetric(password);
            connection.EncodeEnable(encoder);
            return Helper.TrueArray;
        }

        /// <summary>
        /// 清除
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CryptoMessengerIds.Clear)]
        public byte[] Clear(IConnection connection)
        {
            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
            {
                client.Connection.EncodeDisable();
            }
            return Helper.TrueArray;
        }

        /// <summary>
        /// 测试一下
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CryptoMessengerIds.Test)]
        public byte[] Test(IConnection connection)
        {
            Console.WriteLine($"{connection.ServerType},encoder test : {Encoding.UTF8.GetString(connection.Crypto.Decode(connection.ReceiveRequestWrap.Payload).Span)}");

            return Helper.TrueArray;
        }
    }
}
