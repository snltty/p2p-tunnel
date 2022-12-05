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
        /// <param name="arg"></param>
        void Execute(OnPunchHoleArg arg);
    }
    /// <summary>
    /// 打洞数据
    /// </summary>
    public class OnPunchHoleArg
    {
        /// <summary>
        /// 数据
        /// </summary>
        public PunchHoleRequestInfo Data { get; set; }
        /// <summary>
        /// 连接对象
        /// </summary>
        public IConnection Connection { get; set; }
    }
    /// <summary>
    /// 打洞参数
    /// </summary>
    public class ConnectParams
    {
        /// <summary>
        /// 打谁
        /// </summary>
        public ulong Id { get; set; } = 0;
        /// <summary>
        /// 尝试几次
        /// </summary>
        public byte TryTimes { get; set; } = 5;
        /// <summary>
        /// 通道
        /// </summary>
        public ulong TunnelName { get; set; } = 0;
        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { get; set; } = 0;
    }

    /// <summary>
    /// 打洞步骤基类
    /// </summary>
    public class OnStepBaseParams
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
        public PunchHoleNotifyInfo Data { get; set; }
    }
    /// <summary>
    /// 步骤1
    /// </summary>
    public class OnStep1Params : OnStepBaseParams { }
    /// <summary>
    /// 步骤2
    /// </summary>
    public class OnStep2Params : OnStepBaseParams { }
    /// <summary>
    /// 步骤2.1
    /// </summary>
    public class OnStep21Params : OnStepBaseParams { }
    /// <summary>
    /// 步骤2失败
    /// </summary>
    public class OnStep2FailParams : OnStepBaseParams
    {
        /// <summary>
        /// 步骤2失败携带数据
        /// </summary>
        public new PunchHoleStep2FailInfo Data { get; set; }
    }
    /// <summary>
    /// 步骤3
    /// </summary>
    public class OnStep3Params : OnStepBaseParams
    {
        /// <summary>
        /// 步骤3数据
        /// </summary>
        public new PunchHoleStep3Info Data { get; set; }
    }
    /// <summary>
    /// 步骤4
    /// </summary>
    public class OnStep4Params : OnStepBaseParams
    {
        /// <summary>
        /// 步骤4数据
        /// </summary>
        public new PunchHoleStep4Info Data { get; set; }
    }

    /// <summary>
    /// 打洞缓存
    /// </summary>
    public class ConnectCacheModel
    {
        /// <summary>
        /// 尝试几次
        /// </summary>
        public byte TryTimes { get; set; } = 5;
        /// <summary>
        /// 是否已取消
        /// </summary>
        public bool Canceled { get; set; } = false;
        /// <summary>
        /// task
        /// </summary>
        public TaskCompletionSource<ConnectResultModel> Tcs { get; set; }
        /// <summary>
        /// 通道
        /// </summary>
        public ulong TunnelName { get; set; }

        /// <summary>
        /// 步骤1超时
        /// </summary>
        public WheelTimerTimeout<object> Step1Timeout { get; set; }
        /// <summary>
        /// 步骤3超时
        /// </summary>
        public WheelTimerTimeout<object> Step3Timeout { get; set; }

        /// <summary>
        /// 是否已成功
        /// </summary>
        public bool Success { get; set; } = false;

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
