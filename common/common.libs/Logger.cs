using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace common.libs
{
    public class Logger
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => lazy.Value;
        private readonly ConcurrentQueue<LoggerModel> queue = new ConcurrentQueue<LoggerModel>();

        public SimpleSubPushHandler<LoggerModel> OnLogger { get; } = new SimpleSubPushHandler<LoggerModel>();

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

        public void Debug(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.DEBUG, Content = content });
        }
        [Conditional("DEBUG")]
        public void DebugDebug(string content, params object[] args)
        {
            Debug(content, args);
        }
        [Conditional("DEBUG")]
        public void Debug(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.DEBUG, Content = ex + "" });
        }

        public void Info(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.INFO, Content = content });
        }

        public void Warning(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = content });
        }
        public void Warning(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = ex + "" });
        }

        public void Error(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = content });
        }
        public void Error(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = ex + "" });
        }

        [Conditional("DEBUG")]
        public void DebugError(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = content });
        }
         [Conditional("DEBUG")]
        public void DebugError(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = ex + "" });
        }

        public void Enqueue(LoggerModel model)
        {
            queue.Enqueue(model);
        }
    }

    public class LoggerModel
    {
        public LoggerTypes Type { get; set; } = LoggerTypes.INFO;
        public DateTime Time { get; set; } = DateTime.Now;
        public string Content { get; set; } = string.Empty;
    }

    public enum LoggerTypes : byte
    {
        DEBUG = 0, INFO = 1, WARNING = 2, ERROR = 3
    }
}
