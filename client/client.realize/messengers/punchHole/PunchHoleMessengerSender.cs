using client.messengers.punchHole;
using client.messengers.register;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using client.messengers.clients;
using System.Collections.Concurrent;

namespace client.realize.messengers.punchHole
{
    public sealed class PunchHoleMessengerSender
    {
        private Dictionary<PunchHoleTypes, IPunchHole> plugins = new Dictionary<PunchHoleTypes, IPunchHole>();

        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly ServiceProvider serviceProvider;

        private NumberSpace numberSpace = new NumberSpace();
        private WheelTimer<TimeoutState> wheelTimer = new WheelTimer<TimeoutState>();
        private ConcurrentDictionary<ulong, WheelTimerTimeout<TimeoutState>> sends = new ConcurrentDictionary<ulong, WheelTimerTimeout<TimeoutState>>();

        public PunchHoleMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState, ServiceProvider serviceProvider)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
            this.serviceProvider = serviceProvider;
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IPunchHole)))
            {
                IPunchHole obj = (IPunchHole)serviceProvider.GetService(item);
                if (!plugins.ContainsKey(obj.Type))
                {
                    plugins.Add(obj.Type, obj);
                }
            }
        }
        public void OnPunchHole(OnPunchHoleArg arg)
        {
            PunchHoleTypes type = (PunchHoleTypes)arg.Data.PunchType;

            if (plugins.ContainsKey(type))
            {
                IPunchHole plugin = plugins[type];
                plugin?.Execute(arg);
            }
        }
        public void OnResponse(PunchHoleResponseInfo response)
        {
            if (sends.TryRemove(response.RequestId, out WheelTimerTimeout<TimeoutState> timeout))
            {
                timeout.Task.State.Tcs.SetResult(true);
            }
        }

        public async Task Response(IConnection connection, PunchHoleRequestInfo request)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                MessengerId = (ushort)PunchHoleMessengerIds.Response,
                Payload = new PunchHoleResponseInfo
                {
                    FromId = registerState.ConnectId,
                    ToId = request.FromId,
                    RequestId = request.RequestId
                }.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<bool> Request<T>(SendPunchHoleArg<T> arg, ulong requestid = 0) where T : IPunchHoleStepInfo
        {
            IPunchHoleStepInfo msg = arg.Data;
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = arg.Connection,
                MessengerId = (ushort)PunchHoleMessengerIds.Request,
                Payload = new PunchHoleRequestInfo
                {
                    RequestId = requestid,
                    Data = msg.ToBytes(),
                    PunchForwardType = msg.ForwardType,
                    FromId = registerState.ConnectId,
                    PunchStep = msg.Step,
                    PunchType = (byte)msg.PunchType,
                    ToId = arg.ToId,
                    TunnelName = arg.TunnelName,
                    Index = arg.Index,
                }.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<bool> RequestReply<T>(SendPunchHoleArg<T> arg) where T : IPunchHoleStepInfo
        {
            ulong requestid = numberSpace.Increment();
            TimeoutState timeoutState = new TimeoutState
            {
                RequestId = requestid,
                Tcs = new TaskCompletionSource<bool>()
            };
            var timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TimeoutState> { Callback = Callback, State = timeoutState }, 2000);
            sends.TryAdd(requestid, timeout);

            bool res = await Request(arg, requestid);
            if (res == false)
            {
                timeoutState.Tcs.SetResult(false);
            }

            return await timeoutState.Tcs.Task.ConfigureAwait(false);
        }
        private void Callback(WheelTimerTimeout<TimeoutState> timeout)
        {
            if (sends.TryRemove(timeout.Task.State.RequestId, out _))
            {
                timeout.Task.State.Tcs.SetResult(false);
            }
        }


        public SimpleSubPushHandler<OnPunchHoleArg> OnReverse { get; } = new SimpleSubPushHandler<OnPunchHoleArg>();
        /// <summary>
        /// 通知其反向连接
        /// </summary>
        /// <param name="toid"></param>
        /// <param name="tryreverse"></param>
        /// <returns></returns>
        public async Task SendReverse(ClientInfo info)
        {
            byte times = info.TryReverseValue;
            await Request(new SendPunchHoleArg<PunchHoleReverseInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = info.Id,
                Data = new PunchHoleReverseInfo { TryReverse = times }
            }).ConfigureAwait(false);
        }

        public SimpleSubPushHandler<PunchHoleTunnelInfo> OnTunnel { get; } = new SimpleSubPushHandler<PunchHoleTunnelInfo>();
        /// <summary>
        /// 通知其开启新通道
        /// </summary>
        /// <param name="toid"></param>
        /// <param name="tunnelName"></param>
        /// <param name="serverType"></param>
        /// <returns></returns>
        public async Task SendTunnel(ulong toid, ulong tunnelName, ServerType serverType)
        {
            await RequestReply(new SendPunchHoleArg<PunchHoleTunnelInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleTunnelInfo { TunnelName = tunnelName, ServerType = serverType }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 通知其重置注册
        /// </summary>
        /// <param name="toid"></param>
        /// <returns></returns>
        public async Task SendReset(ulong toid)
        {
            await Request(new SendPunchHoleArg<PunchHoleResetInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleResetInfo { }
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 通知其掉线
        /// </summary>
        /// <param name="toid"></param>
        /// <returns></returns>
        public async Task SendOffline(ulong toid)
        {
            await RequestReply(new SendPunchHoleArg<PunchHoleOfflineInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleOfflineInfo { }
            }).ConfigureAwait(false);
        }
    }

    public class SendPunchHoleArg<T>
    {
        public IConnection Connection { get; set; }

        public ulong ToId { get; set; }
        public ulong TunnelName { get; set; } = 0;
        public byte Index { get; set; } = 0;

        public T Data { get; set; }
    }

    public class TimeoutState
    {
        public ulong RequestId { get; set; }
        public TaskCompletionSource<bool> Tcs { get; set; }
    }

}
