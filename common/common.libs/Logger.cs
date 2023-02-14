using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Logger
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        /// <summary>
        /// 
        /// </summary>
        public static Logger Instance => lazy.Value;
        private readonly ConcurrentQueue<LoggerModel> queue = new ConcurrentQueue<LoggerModel>();

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<LoggerModel> OnLogger { get; } = new SimpleSubPushHandler<LoggerModel>();

        /// <summary>
        /// 
        /// </summary>
        public int PaddingWidth { get; set; } = 50;

        private Logger()
        {
            new Thread(() =>
            {
                while (true)
                {
                    while (queue.Count > 0)
                    {
                        if (queue.TryDequeue(out LoggerModel model))
                        {
                            OnLogger.Push(model);
                        }
                    }
                    Thread.Sleep(15);
                }
            })
            { IsBackground = true }.Start();
        }


        public int lockNum = 0;
        public void Lock()
        {
            Interlocked.Increment(ref lockNum);
        }
        public void UnLock()
        {
            Interlocked.Decrement(ref lockNum);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="args"></param>
        public void Debug(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.DEBUG, Content = content });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public void DebugDebug(string content, params object[] args)
        {
            Debug(content, args);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="args"></param>
        public void Info(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.INFO, Content = content });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="args"></param>
        public void Warning(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = content });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public void DebugWarning(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = content });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="args"></param>
        public void Error(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = content });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        public void Error(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = ex + "" });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public void DebugError(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = content });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        [Conditional("DEBUG")]
        public void DebugError(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = ex + "" });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void Enqueue(LoggerModel model)
        {
            queue.Enqueue(model);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LoggerModel
    {
        /// <summary>
        /// 
        /// </summary>
        public LoggerTypes Type { get; set; } = LoggerTypes.INFO;
        /// <summary>
        /// 
        /// </summary>
        public DateTime Time { get; set; } = DateTime.Now;
        /// <summary>
        /// 
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    public enum LoggerTypes : byte
    {
        /// <summary>
        /// 
        /// </summary>
        DEBUG = 0,
        /// <summary>
        /// 
        /// </summary>
        INFO = 1,
        /// <summary>
        /// 
        /// </summary>
        WARNING = 2,
        /// <summary>
        /// 
        /// </summary>
        ERROR = 3
    }
}
