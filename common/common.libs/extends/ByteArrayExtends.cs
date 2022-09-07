using System;
using System.IO;
using System.IO.Compression;

namespace common.libs.extends
{
    public static class ByteArrayExtends
    {
        public static byte[] ToBytes(this int[] obj)
        {
            var bytes = new byte[obj.Length * 4];
            int index = 0;
            for (int i = 0; i < obj.Length; i++)
            {
                Array.Copy(obj[i].ToBytes(), 0, bytes, index, 4);
                index += 4;
            }
            return bytes;
        }
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


        public static byte[] GZip(this byte[] bytes)
        {
            using MemoryStream compressStream = new MemoryStream();
            using var zipStream = new GZipStream(compressStream, CompressionMode.Compress);
            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();//不先关闭会有 解压结果为0的bug
            return compressStream.ToArray();
        }
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
