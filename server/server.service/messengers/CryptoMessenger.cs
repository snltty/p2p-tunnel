using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System;
using System.Text;

namespace server.service.messengers
{
    public class CryptoMessenger : IMessenger
    {
        private readonly IAsymmetricCrypto asymmetricCrypto;
        private readonly ICryptoFactory cryptoFactory;
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly Config config;
        public CryptoMessenger(IAsymmetricCrypto asymmetricCrypto, ICryptoFactory cryptoFactory, IClientRegisterCaching clientRegisterCache, Config config)
        {
            this.asymmetricCrypto = asymmetricCrypto;
            this.cryptoFactory = cryptoFactory;
            this.clientRegisterCache = clientRegisterCache;
            this.config = config;
        }

        public string Key(IConnection connection)
        {
            return asymmetricCrypto.Key.PublicKey;
        }

        public bool Set(IConnection connection)
        {
            string password;
            if (connection.ReceiveRequestWrap.Memory.Length > 0)
            {
                var memory = asymmetricCrypto.Decode(connection.ReceiveRequestWrap.Memory);
                CryptoSetParamsInfo model = memory.DeBytes<CryptoSetParamsInfo>();
                password = model.Password;
            }
            else
            {
                password = config.EncodePassword;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            ISymmetricCrypto encoder = cryptoFactory.CreateSymmetric(password);
            connection.EncodeEnable(encoder);
            return true;
        }
        public bool Test(IConnection connection)
        {
            CryptoTestParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<CryptoTestParamsInfo>();

            Console.WriteLine($"encoder test : {Encoding.UTF8.GetString(connection.Crypto.Decode(model.Content).Span)}");

            return true;
        }
        public bool Clear(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                client.UdpConnection.EncodeDisable();
                client.TcpConnection.EncodeDisable();
            }
            return true;
        }
    }
}
