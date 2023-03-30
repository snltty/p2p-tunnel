using common.libs;
using common.server;
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

        public delegate void StepEvent(object sender, PunchHoleStepModel e);
        public event StepEvent OnStepHandler;

        public Task<ConnectResultModel> Send(ConnectParams param);

        public Task InputData(PunchHoleStepModel model);
    }

    public sealed class PunchHoleStep2TryInfo : IPunchHoleStepInfo
    {
        public PunchHoleTypes PunchType { get; set; } = PunchHoleTypes.TCP_NUTSSB;
        public PunchForwardTypes ForwardType { get; set; } = PunchForwardTypes.NOTIFY;
        public byte Step { get; set; } = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY;
        public byte[] ToBytes()
        {
            return new byte[] {
                (byte)PunchType,
                (byte)ForwardType,
                Step
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
    public sealed class PunchHoleStep2StopInfo : IPunchHoleStepInfo
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
