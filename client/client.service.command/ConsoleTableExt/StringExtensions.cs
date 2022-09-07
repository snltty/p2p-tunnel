using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleTableExt
{
    public static class StringExtensions
    {
        public static int RealLength(this string value,bool withUtf8Characters)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            if (!withUtf8Characters)
                return value.Length;

            int i = 0;//count
            foreach (char newChar in value)
            {
                if (newChar >= 0x4e00 && newChar <= 0x9fbb)
                {
                    //utf-8 characters
                    i += 2;
                }
                else
                {
                    i++;
                }
            }
            return i;
        }
    }
}
