using common.libs;
using System;
using System.Collections.Generic;
using System.IO;

namespace client.service.ftp.extends
{
    public static class StringExtends
    {
        public static List<string> ClearDir(this string dir, string baseDir, string rootDir = "")
        {
            List<string> errs = new List<string>();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                foreach (var item in dir.Split(Helper.SeparatorChar))
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        if (Path.IsPathRooted(dir))
                        {
                            Clear(dir);
                        }
                        else
                        {
                            string filePath = Path.Combine(baseDir, item);
                            if (filePath.StartsWith(rootDir))
                            {
                                Clear(Path.Combine(baseDir, item));
                            }
                            else
                            {
                                errs.Add($"{item} 无目录权限");
                            }
                        }
                    }
                }
            }
            return errs;
        }

        public static List<string> CreateDir(this string dir, string baseDir, string rootDir = "")
        {
            List<string> errs = new List<string>();
            if (!string.IsNullOrWhiteSpace(dir))
            {
                foreach (string item in dir.Split(Helper.SeparatorChar))
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        if (Path.IsPathRooted(dir))
                        {
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                        }
                        else
                        {
                            string filePath = Path.Combine(baseDir, item);
                            if (filePath.StartsWith(rootDir))
                            {
                                if (!Directory.Exists(filePath))
                                {
                                    Directory.CreateDirectory(filePath);
                                }
                            }
                            else
                            {
                                errs.Add($"{item} 无目录权限");
                            }
                        }
                    }
                }
            }
            return errs;
        }

        private static void Clear(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo[] dirs = new DirectoryInfo(path).GetDirectories();
                foreach (DirectoryInfo item in dirs)
                {
                    Clear(item.FullName);
                }

                System.IO.FileInfo[] files = new DirectoryInfo(path).GetFiles();
                foreach (System.IO.FileInfo item in files)
                {
                    item.FullName.DeleteDirectory();
                }
                path.DeleteDirectory();
            }
            else if (File.Exists(path))
            {
                path.DeleteFile();
            }
        }

        private static void DeleteDirectory(this string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    Directory.Delete(path);
                    //FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception)
                {


                }
            }
        }

        private static void DeleteFile(this string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    File.Delete(path);
                    //FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception)
                {

                    //File.Delete(path);
                }
            }
        }
    }
}
