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
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly Process proc = ProcessHelper.GetCurrentProcess();
        private readonly DateTime startTime = DateTime.Now;

        public CounterMessenger(IClientRegisterCaching clientRegisterCaching)
        {
            this.clientRegisterCaching = clientRegisterCaching;
        }

        public CommonResponseInfo<CounterResultInfo> Info(IConnection connection)
        {
            proc.Refresh();

            var clients = clientRegisterCaching.GetAll();
            return new CommonResponseInfo<CounterResultInfo>
            {
                Data = new CounterResultInfo
                {
                    OnlineCount = clientRegisterCaching.Count(),
                    Cpu = ProcessHelper.GetCpu(proc),
                    Memory = ProcessHelper.GetMemory(proc),
                    RunTime = (int)(DateTime.Now - startTime).TotalSeconds,
                    TcpSendBytes = clients.Sum(c => (decimal)(c.TcpConnection?.SendBytes ?? 0)),
                    TcpReceiveBytes = clients.Sum(c => (decimal)(c.TcpConnection?.ReceiveBytes ?? 0)),
                    UdpSendBytes = clients.Sum(c => (decimal)(c.UdpConnection?.SendBytes ?? 0)),
                    UdpReceiveBytes = clients.Sum(c => (decimal)(c.UdpConnection?.ReceiveBytes ?? 0)),
                }
            };
        }
    }


}
