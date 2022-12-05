using common.libs;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.messengers.punchHole.udp
{
    /// <summary>
    /// udp打洞
    /// </summary>
    public interface IPunchHoleUdp
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
        /// <param name="arg"></param>
        /// <returns></returns>
        public Task OnStep1(OnStep1Params arg);

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
        public SimpleSubPushHandler<OnStep21Params> OnStep21Handler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Task OnStep21(OnStep21Params e);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void OnStep2Fail(OnStep2FailParams e);

        /// <summary>
        /// 
        /// </summary>
        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Task OnStep3(OnStep3Params e);

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
    /// udp打洞步骤
    /// </summary>
    [Flags]
    public enum PunchHoleUdpSteps : byte
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
        STEP_2_1 = 3,
        /// <summary>
        /// 
        /// </summary>
        STEP_2_Fail = 4,
        /// <summary>
        /// 
        /// </summary>
        STEP_3 = 5,
        /// <summary>
        /// 
        /// </summary>
        STEP_4 = 6,
    }

    /// <summary>
    /// 
    /// </summary>
    public class PunchHoleStep21Info : IPunchHoleStepInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.UDP;

        /// <summary>
        /// 
        /// </summary>
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;
        /// <summary>
        /// 
        /// </summary>
        public byte Step { get; set; } = (byte)PunchHoleUdpSteps.STEP_2_1;

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

}
