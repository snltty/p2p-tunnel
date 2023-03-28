using common.libs;
using common.server;
using common.server.model;
using server.messengers.singnin;
using System;
using System.Diagnostics;

namespace server.service.messengers
{
    /// <summary>
    /// 服务器信息
    /// </summary>
    [MessengerIdRange((ushort)CounterMessengerIds.Min, (ushort)CounterMessengerIds.Max)]
    public sealed class CounterMessenger : IMessenger
    {
        private readonly Process proc = ProcessHelper.GetCurrentProcess();
        private readonly DateTime startTime = DateTime.Now;
        private CounterResultInfo counterResultInfo = new CounterResultInfo();

        public CounterMessenger(IClientSignInCaching clientSignInCaching, WheelTimer<object> wheelTimer)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                Callback = (state) =>
                {
                    proc.Refresh();
                    counterResultInfo.OnlineCount = clientSignInCaching.Count;
                    counterResultInfo.Cpu = ProcessHelper.GetCpu(proc);
                    counterResultInfo.Memory = ProcessHelper.GetMemory(proc);
                    counterResultInfo.RunTime = (int)(DateTime.Now - startTime).TotalSeconds;
                }
            }, 1000, true);
        }

        [MessengerId((ushort)CounterMessengerIds.Info)]
        public void Info(IConnection connection)
        {
            connection.Write(counterResultInfo.ToBytes());
        }
    }


}
