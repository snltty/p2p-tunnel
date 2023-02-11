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
        public delegate void StepEvent(object sender, PunchHoleStepModel e);
        public event StepEvent OnStepHandler;

        public Task<ConnectResultModel> Send(ConnectParams param);

        public Task InputData(PunchHoleStepModel model);
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
