using common.libs;
using common.server;
using common.server.model;
using server.messengers.register;
using server.service.manager.models;
using System;
using System.Diagnostics;
using System.Linq;

namespace server.service.manager
{
    public class CounterMessenger : IMessenger
    {
        private readonly Process proc = ProcessHelper.GetCurrentProcess();
        private readonly DateTime startTime = DateTime.Now;
        private CounterResultInfo counterResultInfo;

        public CounterMessenger(IClientRegisterCaching clientRegisterCaching, WheelTimer<object> wheelTimer)
        {
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                Callback = (state) =>
                {
                    proc.Refresh();
                    var clients = clientRegisterCaching.GetAll();

                    counterResultInfo = new CounterResultInfo
                    {
                        OnlineCount = clientRegisterCaching.Count,
                        Cpu = ProcessHelper.GetCpu(proc),
                        Memory = ProcessHelper.GetMemory(proc),
                        RunTime = (int)(DateTime.Now - startTime).TotalSeconds,
                        TcpSendBytes = clients.Sum(c => (c.TcpConnection?.SendBytes ?? 0)),
                        TcpReceiveBytes = clients.Sum(c => (c.TcpConnection?.ReceiveBytes ?? 0)),
                        UdpSendBytes = clients.Sum(c => (c.UdpConnection?.SendBytes ?? 0)),
                        UdpReceiveBytes = clients.Sum(c => (c.UdpConnection?.ReceiveBytes ?? 0)),
                    };
                }
            }, 1000, true);
        }

        public byte[] Info(IConnection connection)
        {
            return counterResultInfo.ToBytes();
        }
    }


}
