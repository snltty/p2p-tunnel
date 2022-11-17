using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Text;

namespace client.realize.messengers.crypto
{
    [MessengerIdRange((ushort)CryptoMessengerIds.Min,(ushort)CryptoMessengerIds.Max)]
    public class CryptoMessenger : IMessenger
    {
        private readonly IAsymmetricCrypto asymmetricCrypto;
        private readonly ICryptoFactory cryptoFactory;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly Config config;

        public CryptoMessenger(IAsymmetricCrypto asymmetricCrypto, ICryptoFactory cryptoFactory, IClientInfoCaching clientInfoCaching, Config config)
        {
            this.asymmetricCrypto = asymmetricCrypto;
            this.cryptoFactory = cryptoFactory;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
        }

        [MessengerId((ushort)CryptoMessengerIds.Key)]
        public byte[] Key(IConnection connection)
        {
            return asymmetricCrypto.Key.PublicKey.ToBytes();
        }

        [MessengerId((ushort)CryptoMessengerIds.Set)]
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
                password = config.Client.EncodePassword;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return false.ToBytes();
            }

            ISymmetricCrypto encoder = cryptoFactory.CreateSymmetric(password);
            connection.EncodeEnable(encoder);
            return true.ToBytes();
        }

        [MessengerId((ushort)CryptoMessengerIds.Clear)]
        public bool Clear(IConnection connection)
        {
            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
            {
                client.Connection.EncodeDisable();
            }
            return true;
        }

        [MessengerId((ushort)CryptoMessengerIds.Test)]
        public bool Test(IConnection connection)
        {
            Console.WriteLine($"{connection.ServerType},encoder test : {Encoding.UTF8.GetString(connection.Crypto.Decode(connection.ReceiveRequestWrap.Payload).Span)}");

            return true;
        }
    }
}
