using common.libs;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole.tcp
{
    /// <summary>
    /// tcp打洞
    /// </summary>
    public interface IPunchHoleTcp
    {
        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<ConnectParams> OnSendHandler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public Task<ConnectResultModel> Send(ConnectParams param);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Task OnStep1(OnStep1Params e);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Task OnStep2(OnStep2Params e);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep2RetryParams> OnStep2RetryHandler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void OnStep2Retry(OnStep2RetryParams e);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler { get; }
        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public void OnStep2Fail(OnStep2FailParams arg);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toid"></param>
        /// <returns></returns>
        public Task SendStep2Stop(ulong toid);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void OnStep2Stop(OnStep2StopParams e);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public Task OnStep3(OnStep3Params arg);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public void OnStep4(OnStep4Params arg);
    }


    /// <summary>
    /// 
    /// </summary>
    public class OnStep2RetryParams : OnStepBaseParams
    {
    }
    /// <summary>
    /// 
    /// </summary>
    public class OnStep2StopParams : OnStepBaseParams { }

    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleStep2TryInfo : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;
        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleStep2StopInfo : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.FORWARD;

        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

    /// <summary>
    /// tcp打洞步骤
    /// </summary>
    public enum PunchHoleTcpNutssBSteps : byte
    {
        /// <summary>
        /// 
        /// </summary>
        STEP_1 = 1,
        /// <summary>
        /// 
        /// </summary>
        STEP_2 = 2,
        /// <summary>
        /// 
        /// </summary>
        STEP_2_TRY = 3,
        /// <summary>
        /// 
        /// </summary>
        STEP_2_FAIL = 4,
        /// <summary>
        /// 
        /// </summary>
        STEP_2_STOP = 5,
        /// <summary>
        /// 
        /// </summary>
        STEP_3 = 6,
        /// <summary>
        /// 
        /// </summary>
        STEP_4 = 7,
        /// <summary>
        /// 
        /// </summary>
        STEP_PACKET = 8,
    }

}
