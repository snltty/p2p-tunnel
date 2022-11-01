using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System.Text;

namespace server.service.messengers
{
    [MessengerIdRange((int)CryptoMessengerIds.Min,(int)CryptoMessengerIds.Max)]
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

        [MessengerId((int)CryptoMessengerIds.Key)]
        public byte[] Key(IConnection connection)
        {
            return asymmetricCrypto.Key.PublicKey.ToBytes();
        }

        [MessengerId((int)CryptoMessengerIds.Set)]
        public byte[] Set(IConnection connection)
        {
            string password;
            if (connection.ReceiveRequestWrap.Payload.Length > 0)
            {
                var memory = asymmetricCrypto.Decode(connection.ReceiveRequestWrap.Payload);
                password = memory.GetString();
            }
            else
            {
                password = config.EncodePassword;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return Helper.FalseArray;
            }

            ISymmetricCrypto encoder = cryptoFactory.CreateSymmetric(password);
            connection.EncodeEnable(encoder);
            return Helper.TrueArray;
        }

        [MessengerId((int)CryptoMessengerIds.Test)]
        public byte[] Test(IConnection connection)
        {
            Logger.Instance.DebugDebug($"encoder test : {Encoding.UTF8.GetString(connection.Crypto.Decode(connection.ReceiveRequestWrap.Payload).Span)}");
            return Helper.TrueArray;
        }

        [MessengerId((int)CryptoMessengerIds.Clear)]
        public byte[] Clear(IConnection connection)
        {
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo client))
            {
                client.UdpConnection?.EncodeDisable();
                client.TcpConnection?.EncodeDisable();
            }
            return Helper.FalseArray;
        }
    }
}
