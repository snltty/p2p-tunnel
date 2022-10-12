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

namespace client.realize.messengers.punchHole
{
    public class PunchHoleMessengerSender
    {
        private Dictionary<PunchHoleTypes, IPunchHole> plugins = new Dictionary<PunchHoleTypes, IPunchHole>();

        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly ServiceProvider serviceProvider;

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

        public async Task Send<T>(SendPunchHoleArg<T> arg) where T : IPunchHoleStepInfo
        {
            IPunchHoleStepInfo msg = arg.Data;
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = arg.Connection,
                Path = "punchhole/Execute",
                Memory = new PunchHoleParamsInfo
                {
                    Data = msg.ToBytes(),
                    PunchForwardType = msg.ForwardType,
                    FromId = arg.Connection.ConnectId,
                    PunchStep = msg.Step,
                    PunchType = (byte)msg.PunchType,
                    ToId = arg.ToId,
                    TunnelName = arg.TunnelName,
                    GuessPort = arg.GuessPort,
                    Index = arg.Index,
                }.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> SendReply<T>(SendPunchHoleArg<T> arg) where T : IPunchHoleStepInfo
        {
            IPunchHoleStepInfo msg = arg.Data;
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = arg.Connection,
                Path = "punchhole/Execute",
                Memory = new PunchHoleParamsInfo
                {
                    Data = msg.ToBytes(),
                    PunchForwardType = msg.ForwardType,
                    FromId = arg.Connection.ConnectId,
                    PunchStep = msg.Step,
                    PunchType = (byte)msg.PunchType,
                    ToId = arg.ToId,
                    TunnelName = arg.TunnelName,
                    GuessPort = arg.GuessPort,
                    Index = arg.Index,
                }.ToBytes()
            }).ConfigureAwait(false);
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
            await Send(new SendPunchHoleArg<PunchHoleReverseInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = info.Id,
                Data = new PunchHoleReverseInfo { TryReverse = times }
            }).ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnRelayParam> OnRelay { get; } = new SimpleSubPushHandler<OnRelayParam>();
        /// <summary>
        /// 通知其要使用中继
        /// </summary>
        /// <param name="toid"></param>
        /// <param name="serverType"></param>
        /// <returns></returns>
        public async Task SendRelay(ulong toid, ServerType serverType)
        {
            await Send(new SendPunchHoleArg<PunchHoleRelayInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleRelayInfo { ServerType = serverType }
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
            await SendReply(new SendPunchHoleArg<PunchHoleTunnelInfo>
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
            await Send(new SendPunchHoleArg<PunchHoleResetInfo>
            {
                Connection = registerState.OnlineConnection,
                ToId = toid,
                Data = new PunchHoleResetInfo { }
            }).ConfigureAwait(false);
        }
    }

    public class OnRelayParam
    {
        public OnPunchHoleArg Raw { get; set; }
        public PunchHoleRelayInfo Relay { get; set; }
    }

    public class SendPunchHoleArg<T>
    {
        public IConnection Connection { get; set; }

        public ulong ToId { get; set; }

        public int GuessPort { get; set; } = 0;

        public ulong TunnelName { get; set; } = 0;
        public byte Index { get; set; } = 0;

        public T Data { get; set; }
    }


}
