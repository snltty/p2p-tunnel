using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Text;
using System.Threading.Tasks;

namespace client.realize.messengers.crypto
{
    public sealed class CryptoSwap
    {
        private readonly MessengerSender messengerSender;
        private readonly ICryptoFactory cryptoFactory;

        public CryptoSwap(MessengerSender messengerSender, ICryptoFactory cryptoFactory)
        {
            this.messengerSender = messengerSender;
            this.cryptoFactory = cryptoFactory;
        }

        public async Task<ICrypto> Swap(IConnection tcp, IConnection udp, string password)
        {
            try
            {
                byte[] encodedData = Helper.EmptyArray;
                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageResponeInfo publicKeyResponse = await messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = tcp ?? udp,
                        MessengerId = (ushort)CryptoMessengerIds.Key,
                        Payload = Helper.EmptyArray
                    }).ConfigureAwait(false);
                    if (publicKeyResponse.Code != MessageResponeCodes.OK)
                    {
                        return null;
                    }

                    string publicKey = publicKeyResponse.Data.GetString();
                    IAsymmetricCrypto encoder = cryptoFactory.CreateAsymmetric(new RsaKey { PublicKey = publicKey, PrivateKey = string.Empty });
                    password = StringHelper.RandomPasswordStringMd5();
                    encodedData = encoder.Encode(password.ToBytes());
                    encoder.Dispose();
                }

                ICrypto crypto = cryptoFactory.CreateSymmetric(password);
                if (tcp != null)
                {
                    MessageResponeInfo setResponse = await messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = tcp,
                        MessengerId = (ushort)CryptoMessengerIds.Set,
                        Payload = encodedData
                    }).ConfigureAwait(false);
                    if (setResponse.Code != MessageResponeCodes.OK || crypto.Decode(setResponse.Data.ToArray()).GetBool() == false)
                    {
                        return null;
                    }
                }
                if (udp != null)
                {
                    MessageResponeInfo setResponse = await messengerSender.SendReply(new MessageRequestWrap
                    {
                        Connection = udp,
                        MessengerId = (ushort)CryptoMessengerIds.Set,
                        Payload = encodedData
                    }).ConfigureAwait(false);
                    if (setResponse.Code != MessageResponeCodes.OK || crypto.Decode(setResponse.Data.ToArray()).GetBool() == false)
                    {
                        return null;
                    }
                }

                return crypto;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
                return null;
            }
        }

        public async Task<bool> Test(IConnection connection)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)CryptoMessengerIds.Test,
                Payload = connection.Crypto.Encode(Encoding.UTF8.GetBytes("test"))
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK;
        }
    }
}
