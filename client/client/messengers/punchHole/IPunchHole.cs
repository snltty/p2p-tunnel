using common.libs;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole
{
    /// <summary>
    /// 打洞接口
    /// </summary>
    public interface IPunchHole
    {
        /// <summary>
        /// 打洞类型
        /// </summary>
        PunchHoleTypes Type { get; }
        /// <summary>
        /// 执行局接口
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        Task Execute(IConnection connection,PunchHoleRequestInfo info);
    }
    /// <summary>
    /// 打洞参数
    /// </summary>
    public class ConnectParams
    {
        /// <summary>
        /// 打谁
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 通道
        /// </summary>
        public byte NewTunnel { get; set; }
        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { get; set; }
    }

    /// <summary>
    /// 打洞步骤基类
    /// </summary>
    public class PunchHoleStepModel
    {
        /// <summary>
        /// 连接对象
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 元数据
        /// </summary>
        public PunchHoleRequestInfo RawData { get; set; }
        /// <summary>
        /// 服务端发来的客户端数据
        /// </summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// 打洞缓存
    /// </summary>
    public class ConnectCacheModel
    {
        /// <summary>
        /// 是否已取消
        /// </summary>
        public bool Canceled { get; set; } = false;
        /// <summary>
        /// task
        /// </summary>
        public TaskCompletionSource<ConnectResultModel> Tcs { get; set; }

        /// <summary>
        /// 发送超时
        /// </summary>
        public WheelTimerTimeout<object> SendTimeout { get; set; }

        /// <summary>
        /// 是否已成功
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// 新端口
        /// </summary>
        public byte NewTunnel { get; set; } = 0;

        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { get; set; } = 0;
    }

    /// <summary>
    /// 打洞结果
    /// </summary>
    public class ConnectResultModel
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool State { get; set; }
        /// <summary>
        /// 携带信息
        /// </summary>
        public object Result { get; set; }
    }
    /// <summary>
    /// 打洞失败信息
    /// </summary>
    public class ConnectFailModel
    {
        /// <summary>
        /// 失败原因
        /// </summary>
        public ConnectFailType Type { get; set; } = ConnectFailType.ERROR;
        /// <summary>
        /// 失败原因描述
        /// </summary>
        public string Msg { get; set; } = string.Empty;
    }
    /// <summary>
    /// 打洞失败原因
    /// </summary>
    [Flags]
    public enum ConnectFailType
    {
        /// <summary>
        /// 错误
        /// </summary>
        ERROR,
        /// <summary>
        /// 超时
        /// </summary>
        TIMEOUT,
        /// <summary>
        /// 取消
        /// </summary>
        CANCEL
    }
}
