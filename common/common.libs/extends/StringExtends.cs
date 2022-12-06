using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class StringExtends
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string SubStr(this string str, int start, int maxLength)
        {
            if (maxLength + start > str.Length)
            {
                maxLength = str.Length - start;
            }
            return str.Substring(start, maxLength);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Md5(this string input)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static int ToBytes(this string str, Memory<byte> bytes)
        {
            return Encoding.UTF8.GetBytes(str, bytes.Span);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this Span<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this ReadOnlySpan<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this Memory<byte> span)
        {
            return Encoding.UTF8.GetString(span.Span);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this ReadOnlyMemory<byte> span)
        {
            return Encoding.UTF8.GetString(span.Span);
        }

    }
}
