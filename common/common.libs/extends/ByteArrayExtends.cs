using System;
using System.IO;
using System.IO.Compression;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class ByteArrayExtends
    {
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] GZip(this byte[] bytes)
        {
            using MemoryStream compressStream = new MemoryStream();
            using var zipStream = new GZipStream(compressStream, CompressionMode.Compress);
            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();//不先关闭会有 解压结果为0的bug
            return compressStream.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] UnGZip(this byte[] bytes)
        {
            using var compressStream = new MemoryStream(bytes);
            using var zipStream = new GZipStream(compressStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
}
