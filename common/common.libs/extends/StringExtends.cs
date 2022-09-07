using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;

namespace common.libs.extends
{
    public static class StringExtends
    {
        public static string SubStr(this string str, int start, int maxLength)
        {
            if (maxLength + start > str.Length)
            {
                maxLength = str.Length - start;
            }
            return str.Substring(start, maxLength);
        }

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

        public static T Convert<T>(this string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null)
                {
                    return (T)converter.ConvertFromString(input);
                }
                return default;
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static object Convert(this string input, Type type)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(type);
                if (converter != null)
                {
                    return converter.ConvertFromString(input);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static int ToInt(this string input, int defaultValue = 0)
        {
            if (int.TryParse(input, out int res) == false)
            {
                res = defaultValue;
            }

            return res;
        }

        public static float ToFloat(this string input, float defaultValue = 0)
        {
            if (float.TryParse(input, out float res) == false)
            {
                res = defaultValue;
            }

            return res;
        }

        public static double ToDouble(this string input, double defaultValue = 0)
        {
            if (double.TryParse(input, out double res) == false)
            {
                res = defaultValue;
            }

            return res;
        }

        public static int[] ToIntArray(this string input)
        {
            return Array.ConvertAll(input.Split(Helper.SeparatorChar), c => int.Parse(c));
        }

        public static byte[] ToBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
        public static string GetString(this Span<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        public static string GetString(this ReadOnlySpan<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        public static string GetString(this Memory<byte> span)
        {
            return Encoding.UTF8.GetString(span.Span);
        }
        public static string GetString(this ReadOnlyMemory<byte> span)
        {
            return Encoding.UTF8.GetString(span.Span);
        }

    }
}
