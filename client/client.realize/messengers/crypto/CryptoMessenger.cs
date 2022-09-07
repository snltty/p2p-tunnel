using client.messengers.clients;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Text;

namespace client.realize.messengers.crypto
{
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

        public byte[] Key(IConnection connection)
        {
            return asymmetricCrypto.Key.PublicKey.ToBytes();
        }

        public byte[] Set(IConnection connection)
        {
            string password;
            if (connection.ReceiveRequestWrap.Memory.Length > 0)
            {
                var memory = asymmetricCrypto.Decode(connection.ReceiveRequestWrap.Memory);
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
        public bool Clear(IConnection connection)
        {
            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
            {
                client.UdpConnection.EncodeDisable();
                client.TcpConnection.EncodeDisable();
            }
            return true;
        }

        public bool Test(IConnection connection)
        {
            Console.WriteLine($"{connection.ServerType},encoder test : {Encoding.UTF8.GetString(connection.Crypto.Decode(connection.ReceiveRequestWrap.Memory).Span)}");

            return true;
        }
    }
}
