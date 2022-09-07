using common.libs;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole.tcp
{
    public interface IPunchHoleTcp
    {
        public SimpleSubPushHandler<ConnectParams> OnSendHandler { get; }
        public Task<ConnectResultModel> Send(ConnectParams param);

        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; }
        public Task OnStep1(OnStep1Params e);

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; }
        public Task OnStep2(OnStep2Params e);

        public SimpleSubPushHandler<OnStep2RetryParams> OnStep2RetryHandler { get; }
        public void OnStep2Retry(OnStep2RetryParams e);

        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler { get; }
        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; }
        public void OnStep2Fail(OnStep2FailParams arg);

        public Task SendStep2Stop(ulong toid);
        public void OnStep2Stop(OnStep2StopParams e);

        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; }
        public Task OnStep3(OnStep3Params arg);

        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; }
        public void OnStep4(OnStep4Params arg);
    }


    public class OnStep2RetryParams : OnStepBaseParams { }
    public class OnStep2StopParams : OnStepBaseParams { }

    public class PunchHoleStep2TryInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;

        public byte Step { get; set; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY;

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }
    public class PunchHoleStep2StopInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;

        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.FORWARD;

        public byte Step { get; set; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP;

        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step,
            };
        }
        public void DeBytes(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            PunchType = (PunchHoleTypes)span[0];
            ForwardType = (PunchForwardTypes)span[1];
            Step = span[2];
        }
    }

    public enum PunchHoleTcpNutssBSteps : byte
    {
        STEP_1 = 1,
        STEP_2 = 2,
        STEP_2_TRY = 3,
        STEP_2_FAIL = 4,
        STEP_2_STOP = 5,
        STEP_3 = 6,
        STEP_4 = 7,
        STEP_PACKET = 8,
    }

}
