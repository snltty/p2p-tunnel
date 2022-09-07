using common.libs;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole
{
    public interface IPunchHole
    {
        PunchHoleTypes Type { get; }
        void Execute(OnPunchHoleArg arg);
    }
    public class OnPunchHoleArg
    {
        public PunchHoleParamsInfo Data { get; set; }
        public IConnection Connection { get; set; }
    }

    public class ConnectParams
    {
        public ulong Id { get; set; } = 0;
        public int TryTimes { get; set; } = 5;
        public string TunnelName { get; set; } = string.Empty;
    }

    public class OnStepBaseParams
    {
        public IConnection Connection { get; set; }
        public PunchHoleParamsInfo RawData { get; set; }
        public PunchHoleNotifyInfo Data { get; set; }
    }
    public class OnStep1Params : OnStepBaseParams { }
    public class OnStep2Params : OnStepBaseParams { }
    public class OnStep21Params : OnStepBaseParams { }
    public class OnStep2FailParams : OnStepBaseParams
    {
        public new PunchHoleStep2FailInfo Data { get; set; }
    }
    public class OnStep3Params : OnStepBaseParams
    {
        public new PunchHoleStep3Info Data { get; set; }
    }
    public class OnStep4Params : OnStepBaseParams
    {
        public new PunchHoleStep4Info Data { get; set; }
    }

    public class ConnectCacheModel
    {
        public int TryTimes { get; set; } = 5;
        public bool Canceled { get; set; } = false;
        public TaskCompletionSource<ConnectResultModel> Tcs { get; set; }
        public string TunnelName { get; set; }

        public WheelTimerTimeout<object> Step1Timeout { get; set; }
        public WheelTimerTimeout<object> Step3Timeout { get; set; }
    }
    public class ConnectResultModel
    {
        public bool State { get; set; }
        public object Result { get; set; }
    }
    public class ConnectFailModel
    {
        public ConnectFailType Type { get; set; } = ConnectFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }
    [Flags]
    public enum ConnectFailType
    {
        ERROR, TIMEOUT, CANCEL
    }
}
