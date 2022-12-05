using common.libs;
using common.server;
using common.server.model;
using server.messengers.register;
using System;
using System.Diagnostics;
using System.Linq;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCaching"></param>
        /// <param name="wheelTimer"></param>
        public CounterMessenger(IClientRegisterCaching clientRegisterCaching, WheelTimer<object> wheelTimer)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                Callback = (state) =>
                {
                    proc.Refresh();
                    var clients = clientRegisterCaching.Get();
                    counterResultInfo.OnlineCount = clientRegisterCaching.Count;
                    counterResultInfo.Cpu = ProcessHelper.GetCpu(proc);
                    counterResultInfo.Memory = ProcessHelper.GetMemory(proc);
                    counterResultInfo.RunTime = (int)(DateTime.Now - startTime).TotalSeconds;
                    counterResultInfo.TcpSendBytes = clients.Sum(c => (c.TcpConnection?.SendBytes ?? 0));
                    counterResultInfo.TcpReceiveBytes = clients.Sum(c => (c.TcpConnection?.ReceiveBytes ?? 0));
                    counterResultInfo.UdpSendBytes = clients.Sum(c => (c.UdpConnection?.SendBytes ?? 0));
                    counterResultInfo.UdpReceiveBytes = clients.Sum(c => (c.UdpConnection?.ReceiveBytes ?? 0));
                }
            }, 1000, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)CounterMessengerIds.Info)]
        public byte[] Info(IConnection connection)
        {
            return counterResultInfo.ToBytes();
        }
    }


}
