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
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this int[] obj)
        {
            var bytes = new byte[obj.Length * 4];
            obj.ToBytes(bytes);
            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int[] DeBytes2IntArray(this ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            int[] res = new int[data.Length / 4];
            int index = 0;
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = span.Slice(index, 4).ToInt32();
                index += 4;
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this ushort[] obj)
        {
            var bytes = new byte[obj.Length * 2];
            obj.ToBytes(bytes);
            return bytes;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ushort[] DeBytes2UInt16Array(this ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            ushort[] res = new ushort[data.Length / 2];
            int index = 0;
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = span.Slice(index, 2).ToUInt16();
                index += 2;
            }
            return res;
        }

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
