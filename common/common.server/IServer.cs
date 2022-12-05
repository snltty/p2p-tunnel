using common.libs;
using System;
using System.Threading.Tasks;

namespace common.server
{
    /// <summary>
    /// 服务
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public void Start(int port);
        /// <summary>
        /// 
        /// </summary>
        public void Stop();
        /// <summary>
        /// 
        /// </summary>
        public void Disponse();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public Task InputData(IConnection connection);

        /// <summary>
        /// 收到包
        /// </summary>
        public Func<IConnection, Task> OnPacket { get; set; }
        /// <summary>
        /// 有人离线
        /// </summary>
        public SimpleSubPushHandler<IConnection> OnDisconnect { get; }
        /// <summary>
        /// 有人上线
        /// </summary>
        public Action<IConnection> OnConnected { get; set; }
    }
}
