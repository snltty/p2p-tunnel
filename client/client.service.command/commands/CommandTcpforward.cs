using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;

namespace client.service.command.commands
{
    internal class CommandTcpforward : CommandBase
    {
        private Dictionary<string, string> tunnelTypes = new Dictionary<string, string> { { "2", "tcp" }, { "4", "udp" }, { "8", "tcp first" }, { "16", "udp first" } };
        public CommandTcpforward(RootCommand rootCommand)
        {
            string tunnelTypesStr = string.Join(",", tunnelTypes.Select(c => $"{c.Key}:{c.Value}"));

            Command tcpforward = new Command("tcpforward", "tcp转发相关命令") { };
            rootCommand.Add(tcpforward);


            Command list = new Command("list", "查看列表") { };
            tcpforward.Add(list);
            list.SetHandler(HandlerList);


            Command add = new Command("add", "添加监听") { };
            tcpforward.Add(add);
            Option<int> addPort = new Option<int>("--port", description: "端口") { IsRequired = true };
            add.AddOption(addPort);
            Option<int> addAliveType = new Option<int>("--alive", description: $"连接类型，{(int)TcpForwardAliveTypes.TUNNEL}长连接，{(int)TcpForwardAliveTypes.WEB}短链接") { IsRequired = true }.FromAmong("1", "2");
            add.AddOption(addAliveType);
            add.SetHandler((addPort, addAliveType) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/AddListen", new
                {
                    id = 0,
                    content = new
                    {
                        ID = 0,
                        Port = addPort,
                        AliveType = addAliveType,
                        ForwardType = (int)TcpForwardTypes.FORWARD,
                    }.ToJsonPipeline()
                }.ToJsonPipeline()));
                PrintRequestState(res);
            }, addPort, addAliveType);

            Command del = new Command("del", "删除监听") { };
            tcpforward.Add(del);
            var delid = new Argument<int>("id", "id");
            del.Add(delid);
            del.SetHandler((delid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/RemoveListen", new { id = delid, content = string.Empty }.ToJsonPipeline()));
                PrintRequestState(res);
            }, delid);


            Command start = new Command("start", "启动监听") { };
            tcpforward.Add(start);
            var startid = new Argument<int>("id", "id");
            start.Add(startid);
            start.SetHandler((startid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/Start", new { id = startid, content = string.Empty }.ToJsonPipeline()));
                PrintRequestState(res);
            }, startid);


            Command stop = new Command("stop", "停止监听") { };
            tcpforward.Add(stop);
            var stopid = new Argument<int>("id", "id");
            stop.Add(stopid);
            stop.SetHandler((stopid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/Stop", new { id = stopid, content = string.Empty }.ToJsonPipeline()));
                PrintRequestState(res);
            }, stopid);


            Command addForward = new Command("forward-add", "添加转发") { };
            tcpforward.Add(addForward);
            Argument<int> listenid = new Argument<int>("--listenid", description: "监听id");
            addForward.AddArgument(listenid);
            Option<int> addTunnelType = new Option<int>("--tunnel", description: $"通道类型，{tunnelTypesStr}")
            { IsRequired = true }.FromAmong(tunnelTypes.Keys.ToArray());
            addForward.AddOption(addTunnelType);
            Option<string> addName = new Option<string>("--name", description: "目标客户端名") { IsRequired = true };
            addForward.AddOption(addName);
            Option<string> addSourceIp = new Option<string>("--source", description: "访问ip") { IsRequired = true };
            addForward.AddOption(addSourceIp);
            Option<string> addTarget = new Option<string>("--target", description: "目标地址，带端口，例如127.0.0.1:80") { IsRequired = true };
            addForward.AddOption(addTarget);
            addForward.SetHandler((listenid, addTunnelAliveType, addName, addSourceIp, addTarget) =>
            {
                IPEndPoint ip = IPEndPoint.Parse(addTarget);
                JsonNode res = JsonNode.Parse(Request("tcpforward/AddForward", new
                {
                    id = 0,
                    content = new
                    {
                        ListenID = listenid,
                        Forward = new
                        {
                            ID = 0,
                            Name = addName,
                            TunnelType = addTunnelAliveType,
                            SourceIp = addSourceIp,
                            TargetIp = ip.Address.ToString(),
                            TargetPort = ip.Port
                        },
                    }.ToJsonPipeline()
                }.ToJsonPipeline()));
                PrintRequestState(res);
            }, listenid, addTunnelType, addName, addSourceIp, addTarget);

            Command delForward = new Command("forward-del", "删除转发") { };
            tcpforward.Add(delForward);
            var delListenid = new Argument<int>("listenid", "监听id");
            var delForwardid = new Argument<int>("forwardid", "转发id");
            delForward.Add(delListenid);
            delForward.Add(delForwardid);
            delForward.SetHandler((delListenid, delForwardid) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/RemoveForward", new { id = 0, content = new { ListenID = delListenid, ForwardID = delForwardid }.ToJsonPipeline() }.ToJsonPipeline()));
                PrintRequestState(res);
            }, delListenid, delForwardid);


            Command proxy = new Command("proxy-list", "查看http1.1代理") { };
            tcpforward.Add(proxy);
            proxy.SetHandler(() =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/ListProxy"));
                if (res.Root["Code"].GetValue<int>() == 0)
                {
                    var array = res.Root["Content"].AsArray();
                    var forwards = new List<List<object>> {
                            new List<object>{"id","端口", "通道类型", "目标","监听状态","pac代理" }
                        }.Concat(array.Select(c => new List<object> {
                                    c["ID"].ToString() ,
                                    c["Port"].ToString(),
                                    tunnelTypes[c["TunnelType"].ToString()],
                                    string.IsNullOrWhiteSpace(c["Name"].ToString())?"服务器":c["Name"].ToString(),
                                    c["Listening"].GetValue<bool>()?"已监听":"------",
                                    c["IsPac"].GetValue<bool>() ? "已设置":"------",
                                }).ToList()).ToList();
                    PrintTable(forwards);
                }

                PrintRequestState(res);
            });


            Command addProxy = new Command("proxy-add", "添加http1.1代理") { };
            tcpforward.Add(addProxy);
            Option<int> addProxyPort = new Option<int>("--port", description: "端口") { IsRequired = true };
            addProxy.AddOption(addProxyPort);
            Option<int> addProxyTunnelType = new Option<int>("--tunnel", description: $"通道类型，{tunnelTypesStr}", getDefaultValue: () => (int)TcpForwardTunnelTypes.TCP_FIRST).FromAmong(tunnelTypes.Keys.ToArray());
            addProxy.AddOption(addProxyTunnelType);
            Option<string> addProxyName = new Option<string>("--name", description: "目标客户端名，空为服务器", getDefaultValue: () => string.Empty);
            addProxy.AddOption(addProxyName);
            Option<string> addProxyListening = new Option<string>("--listen", description: "是否开启监听", getDefaultValue: () => "false").FromAmong("true", "false");
            addProxy.AddOption(addProxyListening);
            Option<string> addProxyIsPac = new Option<string>("--ispac", description: "是否开启pac代理", getDefaultValue: () => "false").FromAmong("true", "false");
            addProxy.AddOption(addProxyIsPac);
            addProxy.SetHandler((addProxyPort, addProxyTunnelType, addProxyName, addProxyListening, addProxyIsPac) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/AddListen", new
                {
                    id = 0,
                    content = new
                    {
                        ID = 0,
                        Port = addProxyPort,
                        Listening = Boolean.Parse(addProxyListening),
                        Name = addProxyName,
                        TunnelType = addProxyTunnelType,
                        AliveType = (int)TcpForwardAliveTypes.WEB,
                        ForwardType = (int)TcpForwardTypes.PROXY,
                        IsPac = Boolean.Parse(addProxyIsPac)
                    }.ToJsonPipeline()
                }.ToJsonPipeline()));
                PrintRequestState(res);
            }, addProxyPort, addProxyTunnelType, addProxyName, addProxyListening, addProxyIsPac);


            Command updateProxy = new Command("proxy-update", "更新http1.1代理") { };
            tcpforward.Add(updateProxy);
            Option<string> updateProxyPort = new Option<string>("--port", description: "端口", getDefaultValue: getDefaultValue);
            updateProxy.AddOption(updateProxyPort);
            Option<string> updateProxyTunnelType = new Option<string>("--tunnel", description: $"通道类型，{tunnelTypesStr}", getDefaultValue: getDefaultValue).FromAmong(tunnelTypes.Keys.ToArray());
            updateProxy.AddOption(updateProxyTunnelType);
            Option<string> updateProxyName = new Option<string>("--name", description: "目标客户端名，空为服务器", getDefaultValue: getDefaultValue);
            updateProxy.AddOption(updateProxyName);
            Option<string> updateProxyListening = new Option<string>("--listen", description: "是否开启监听", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            updateProxy.AddOption(updateProxyListening);
            Option<string> updateProxyIsPac = new Option<string>("--ispac", description: "是否开启pac代理", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            updateProxy.AddOption(updateProxyIsPac);
            updateProxy.SetHandler((updateProxyPort, updateProxyTunnelType, updateProxyName, updateProxyListening, updateProxyIsPac) =>
            {
                JsonNode res = JsonNode.Parse(Request("tcpforward/ListProxy"));
                if (res.Root["Code"].GetValue<int>() == 0)
                {
                    var array = res.Root["Content"].AsArray();
                    if (array.Count > 0)
                    {
                        JsonNode proxy = array[0];
                        RunAsNotDefaultValue(updateProxyPort, () => { proxy["Port"] = int.Parse(updateProxyPort); });
                        RunAsNotDefaultValue(updateProxyTunnelType, () => { proxy["TunnelType"] = int.Parse(updateProxyTunnelType); });
                        RunAsNotDefaultValue(updateProxyName, () => { proxy["Name"] = updateProxyName; });
                        RunAsNotDefaultValue(updateProxyListening, () => { proxy["Listening"] = bool.Parse(updateProxyListening); });
                        RunAsNotDefaultValue(updateProxyIsPac, () => { proxy["IsPac"] = bool.Parse(updateProxyIsPac); });
                        Request("tcpforward/AddListen", new { id = 0, content = proxy.ToJsonPipeline() }.ToJsonPipeline());
                    }
                    else
                    {
                        Console.WriteLine("Empty");
                    }
                }

                PrintRequestState(res);
            }, updateProxyPort, updateProxyTunnelType, updateProxyName, updateProxyListening, updateProxyIsPac);


        }

        private void HandlerList()
        {
            JsonNode res = JsonNode.Parse(Request("tcpforward/list"));
            if (res.Root["Code"].GetValue<int>() == 0)
            {
                var array = res.Root["Content"].AsArray();
                foreach (var item in array)
                {
                    PrintTable(new List<List<object>> {
                            new List<object>{"id","监听端口", "监听状态", "连接类型" },
                            new List<object>{
                                item["ID"].ToString(),
                                item["Port"].ToString(),
                                item["Listening"].GetValue<bool>() ? "已监听":"--------",
                                item["AliveType"].GetValue<int>() == 1 ?"长连接":"短链接"
                            }
                        });

                    var forwards = new List<List<object>> {
                            new List<object>{"id","访问源", "通道类型", "目标" }
                        }.Concat(item["Forwards"].AsArray().Select(c => new List<object> {
                                    c["ID"].ToString() ,
                                    $"{c["SourceIp"]}:{item["Port"]}" ,
                                    tunnelTypes[c["TunnelType"].ToString()],
                                    $"[{c["Name"]}]{c["TargetIp"]}:{c["TargetPort"]}",
                                }).ToList()).ToList();
                    PrintTable(forwards);
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }

            PrintRequestState(res);
        }
    }

    [Flags]
    public enum TcpForwardTypes : byte
    {
        [Description("转发")]
        FORWARD = 1,
        [Description("代理")]
        PROXY = 2
    }
    [Flags]
    public enum TcpForwardAliveTypes : byte
    {
        [Description("长连接")]
        TUNNEL = 1,
        [Description("短连接")]
        WEB = 2
    }
    [Flags]
    public enum TcpForwardTunnelTypes : byte
    {
        [Description("只tcp")]
        TCP = 1 << 1,
        [Description("只udp")]
        UDP = 1 << 2,
        [Description("优先tcp")]
        TCP_FIRST = 1 << 3,
        [Description("优先udp")]
        UDP_FIRST = 1 << 4,
    }
}
