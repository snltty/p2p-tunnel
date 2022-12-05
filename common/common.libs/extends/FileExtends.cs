using System;
using System.IO;

namespace common.libs.extends
{
    /// <summary>
    /// 
    /// </summary>
    public static class FileExtends
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool TryDeleteFile(this string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }

            }
            return true;
        }
    }
}
