using common.libs.extends;
using System;
using System.Linq;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public static string RandomPasswordString(int len)
        {
            string str = "1234567890abcdefghijklmnopqrstuvwxyz!@#$%^&*()_-+=<,>.?/:;\"'{[}].*";

            Random r = new Random();
            return string.Join(string.Empty, str.OrderBy(x => r.Next(str.Length - 1)).Take(len));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string RandomPasswordStringMd5()
        {
            return string.Format("{0}{1}{2}", RandomPasswordString(32), Guid.NewGuid().ToString(), DateTimeHelper.GetTimeStamp()).Md5();
        }
    }
}
