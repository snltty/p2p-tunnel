using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICryptoFactory
    {
        /// <summary>
        /// 对称加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public ISymmetricCrypto CreateSymmetric(string password);
        /// <summary>
        /// 非对称加密
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IAsymmetricCrypto CreateAsymmetric(RsaKey key);
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class CryptoFactory : ICryptoFactory
    {
        /// <summary>
        /// 对称加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public ISymmetricCrypto CreateSymmetric(string password)
        {
            return new AesCrypto(password);
        }
        /// <summary>
        /// 非对称加密
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IAsymmetricCrypto CreateAsymmetric(RsaKey key)
        {
            return new RsaCrypto(key);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ICrypto : IDisposable
    {
       /// <summary>
       /// 
       /// </summary>
       /// <param name="buffer"></param>
       /// <returns></returns>
        public byte[] Encode(byte[] buffer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public byte[] Encode(in ReadOnlyMemory<byte> buffer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Memory<byte> Decode(byte[] buffer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Memory<byte> Decode(in ReadOnlyMemory<byte> buffer);
    }

    /// <summary>
    /// 非对称加密
    /// </summary>
    public interface IAsymmetricCrypto : ICrypto
    {
        /// <summary>
        /// 
        /// </summary>
        public RsaKey Key { get; }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class RsaCrypto : IAsymmetricCrypto
    {
        RsaKey key = new RsaKey();

        /// <summary>
        /// 
        /// </summary>
        public RsaKey Key => key;

        /// <summary>
        /// 
        /// </summary>
        public RsaCrypto()
        {
            CreateKey();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public RsaCrypto(RsaKey key)
        {
            if (key != null)
            {
                this.key = key;
            }
            else
            {
                CreateKey();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Memory<byte> Decode(byte[] buffer)
        {
            using RSACryptoServiceProvider coder = new RSACryptoServiceProvider();
            coder.FromXmlString(key.PrivateKey);

            int blockLen = coder.KeySize / 8;
            if (buffer.Length <= blockLen)
            {
                return coder.Decrypt(buffer, false);
            }

            using MemoryStream dataStream = new MemoryStream(buffer);
            using MemoryStream enStream = new MemoryStream();

            byte[] data = new byte[blockLen];
            while (true)
            {
                int len = dataStream.Read(data, 0, blockLen);
                if (len == 0) break;

                if (len == blockLen)
                {
                    byte[] enBlock = coder.Decrypt(data, false);
                    enStream.Write(enBlock, 0, enBlock.Length);
                }
                else
                {
                    byte[] block = new byte[len];
                    Array.Copy(data, 0, block, 0, len);

                    byte[] enBlock = coder.Decrypt(block, false);
                    enStream.Write(enBlock, 0, enBlock.Length);
                    break;
                }
            }
            return enStream.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Memory<byte> Decode(in ReadOnlyMemory<byte> buffer)
        {
            return Decode(buffer.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public byte[] Encode(byte[] buffer)
        {
            using RSACryptoServiceProvider coder = new RSACryptoServiceProvider();
            coder.FromXmlString(key.PublicKey);

            int blockLen = coder.KeySize / 8 - 11;
            if (buffer.Length <= blockLen)
            {
                return coder.Encrypt(buffer, false);
            }

            using MemoryStream dataStream = new MemoryStream(buffer);
            using MemoryStream enStream = new MemoryStream();

            byte[] data = new byte[blockLen];
            while (true)
            {
                int len = dataStream.Read(data, 0, blockLen);
                if (len == 0) break;

                if (len == blockLen)
                {
                    byte[] enBlock = coder.Encrypt(data, false);
                    enStream.Write(enBlock, 0, enBlock.Length);
                }
                else
                {
                    byte[] block = new byte[len];
                    Array.Copy(data, 0, block, 0, len);

                    byte[] enBlock = coder.Encrypt(block, false);
                    enStream.Write(enBlock, 0, enBlock.Length);
                    break;
                }
            }

            return enStream.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public byte[] Encode(in ReadOnlyMemory<byte> buffer)
        {
            return Encode(buffer.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            key = null;
        }
        
        private void CreateKey()
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            key.PrivateKey = rsa.ToXmlString(true);
            key.PublicKey = rsa.ToXmlString(false);
        }

    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class RsaKey
    {
        /// <summary>
        /// 
        /// </summary>
        public string PrivateKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string PublicKey { get; set; }
    }

    /// <summary>
    /// 对称加密
    /// </summary>
    public interface ISymmetricCrypto : ICrypto
    {
        public string Password { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class AesCrypto : ISymmetricCrypto
    {
        private ICryptoTransform encryptoTransform;
        private ICryptoTransform decryptoTransform;

        public string Password { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        public AesCrypto(in string password)
        {
            Password = password;
            using Aes aes = Aes.Create();
            aes.Padding = PaddingMode.ANSIX923;
            (aes.Key, aes.IV) = GenerateKeyAndIV(password);

            encryptoTransform = aes.CreateEncryptor(aes.Key, aes.IV);
            decryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public byte[] Encode(byte[] buffer)
        {
            return encryptoTransform.TransformFinalBlock(buffer, 0, buffer.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public byte[] Encode(in ReadOnlyMemory<byte> buffer)
        {
            return Encode(buffer.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Memory<byte> Decode(byte[] buffer)
        {
            return decryptoTransform.TransformFinalBlock(buffer, 0, buffer.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Memory<byte> Decode(in ReadOnlyMemory<byte> buffer)
        {
            return Decode(buffer.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            encryptoTransform.Dispose();
            decryptoTransform.Dispose();
        }

        private (byte[] Key, byte[] IV) GenerateKeyAndIV(in string password)
        {
            byte[] key = new byte[32];
            byte[] iv = new byte[16];

            using SHA384 sha = SHA384.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

            Array.Copy(hash, 0, key, 0, 32);
            Array.Copy(hash, 32, iv, 0, 16);
            return (Key: key, IV: iv);
        }
    }


}
