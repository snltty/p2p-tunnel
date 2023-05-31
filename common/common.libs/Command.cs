using System;
using System.Diagnostics;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Command
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        public static string Windows(string arg, string[] commands)
        {
            return Execute("cmd.exe", arg, commands);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        public static string Linux(string arg, string[] commands)
        {
            return Execute("/bin/bash", arg, commands);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        public static string Osx(string arg, string[] commands)
        {
            return Execute("/bin/bash", arg, commands);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static Process Execute(string fileName, string arg)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Arguments = arg;
            proc.StartInfo.Verb = "runas";
            proc.Start();

            //Process proc = Process.Start(fileName, arg);
            return proc;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arg"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        public static string Execute(string fileName, string arg, string[] commands)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Arguments = arg;
            proc.StartInfo.Verb = "runas";
            proc.Start();

            if (commands.Length > 0)
            {
                for (int i = 0; i < commands.Length; i++)
                {
                    proc.StandardInput.WriteLine(commands[i]);
                }
            }

            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine("exit");
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            proc.Close();
            proc.Dispose();

            return output;
        }
    }
}
