using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using common.libs;
using common.server.model;

namespace client.realize.messengers.punchHole.tcp.nutssb
{
    /// <summary>
    /// tcp打洞消息
    /// </summary>
    public sealed class PunchHoleTcpNutssB : IPunchHole
    {
        private readonly IPunchHoleTcp punchHoleTcp;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="punchHoleTcp"></param>
        public PunchHoleTcpNutssB(IPunchHoleTcp punchHoleTcp)
        {
            this.punchHoleTcp = punchHoleTcp;
        }

        /// <summary>
        /// 
        /// </summary>
        public PunchHoleTypes Type => PunchHoleTypes.TCP_NUTSSB;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        public void Execute(OnPunchHoleArg arg)
        {
            PunchHoleTcpNutssBSteps step = (PunchHoleTcpNutssBSteps)arg.Data.PunchStep;

            Logger.Instance.Debug($"tcp {step}");

            switch (step)
            {
                case PunchHoleTcpNutssBSteps.STEP_1:
                    Step1(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2:
                    Step2(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_TRY:
                    Step2Try(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_FAIL:
                    Step2Fail(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_STOP:
                    Step2Stop(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_3:
                    Step3(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_4:
                    Step4(arg);
                    break;
                default:
                    break;
            }
        }

        private void Step1(OnPunchHoleArg arg)
        {
            PunchHoleNotifyInfo model = new PunchHoleNotifyInfo();
            model.DeBytes(arg.Data.Data);
            punchHoleTcp.OnStep1(new OnStep1Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = model
            });
        }
        private void Step2(OnPunchHoleArg arg)
        {
            PunchHoleNotifyInfo model = new PunchHoleNotifyInfo();
            model.DeBytes(arg.Data.Data);
            punchHoleTcp.OnStep2(new OnStep2Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = model
            });
        }
        private void Step2Try(OnPunchHoleArg arg)
        {

            PunchHoleNotifyInfo model = new PunchHoleNotifyInfo();
            model.DeBytes(arg.Data.Data);
            punchHoleTcp.OnStep2Retry(new OnStep2RetryParams
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = model
            });
        }
        private void Step2Fail(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Fail(new OnStep2FailParams
            {
                Connection = arg.Connection,
                RawData = arg.Data
            });
        }
        private void Step2Stop(OnPunchHoleArg arg)
        {
            punchHoleTcp.OnStep2Stop(new OnStep2StopParams
            {
                Connection = arg.Connection,
                RawData = arg.Data
            });
        }

        private void Step3(OnPunchHoleArg arg)
        {
            PunchHoleStep3Info model = new PunchHoleStep3Info();
            model.DeBytes(arg.Data.Data);

            punchHoleTcp.OnStep3(new OnStep3Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = model
            });
        }
        private void Step4(OnPunchHoleArg arg)
        {
            //Logger.Instance.DebugDebug($"tcp fromid:{arg.Data.FromId},OnStep4");
            PunchHoleStep4Info model = new PunchHoleStep4Info();
            model.DeBytes(arg.Data.Data);
            punchHoleTcp.OnStep4(new OnStep4Params
            {
                Connection = arg.Connection,
                RawData = arg.Data,
                Data = model
            });
        }
    }


}
