using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace client.service.command.commands
{
    internal class CommandSocks5 : CommandBase
    {
        private Dictionary<string, string> tunnelTypes = new Dictionary<string, string> { { "2", "tcp" }, { "4", "udp" }, { "8", "tcp first" }, { "16", "udp first" } };
        public CommandSocks5(RootCommand rootCommand)
        {
            string tunnelTypesStr = string.Join(",", tunnelTypes.Select(c => $"{c.Key}:{c.Value}"));

            Command socks5 = new Command("socks5", "tcp转发相关命令") { };
            rootCommand.Add(socks5);


            Command list = new Command("list", "查看列表") { };
            socks5.Add(list);
            list.SetHandler(() =>
            {
                JsonNode res = JsonNode.Parse(Request("socks5/get"));
                if (res.Root["Code"].GetValue<int>() == 0)
                {
                    JsonNode proxy = res.Root["Content"];
                    Console.WriteLine(proxy.ToJsonString(new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
                    }));
                }

                PrintRequestState(res);
            });

            Command update = new Command("update", "更新socks5代理") { };
            socks5.Add(update);
            Option<string> updatePort = new Option<string>("--port", description: "端口", getDefaultValue: getDefaultValue);
            update.AddOption(updatePort);
            Option<string> updateTunnelType = new Option<string>("--tunnel", description: $"通道类型，{tunnelTypesStr}", getDefaultValue: getDefaultValue).FromAmong(tunnelTypes.Keys.ToArray());
            update.AddOption(updateTunnelType);
            Option<string> updateName = new Option<string>("--name", description: "目标客户端名，空为服务器", getDefaultValue: getDefaultValue);
            update.AddOption(updateName);
            Option<string> updateListening = new Option<string>("--listen", description: "是否开启监听", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            update.AddOption(updateListening);
            Option<string> updateIsPac = new Option<string>("--ispac", description: "是否开启pac代理", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            update.AddOption(updateIsPac);
            Option<string> updateIsConnect = new Option<string>("--connect", description: "是否允许被连接", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            update.AddOption(updateIsConnect);
            Option<string> updateIsLanConnect = new Option<string>("--connect-lan", description: "是否允许被连接局域网地址", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            update.AddOption(updateIsLanConnect);
            Option<string> updateNum = new Option<string>("--num", description: "支持连接数", getDefaultValue: getDefaultValue);
            update.AddOption(updateNum);
            Option<string> updateBufferSize = new Option<string>("--buffersize", description: "buffer size", getDefaultValue: getDefaultValue);
            update.AddOption(updateBufferSize);
            update.SetHandler((JsonNode node) =>
            {
                JsonNode res = JsonNode.Parse(Request("socks5/get"));
                if (res.Root["Code"].GetValue<int>() == 0)
                {
                    JsonNode proxy = res.Root["Content"];
                    RunAsNotDefaultValue(node["updatePort"].ToString(), () => { proxy["ListenPort"] = int.Parse(node["updatePort"].ToString()); });
                    RunAsNotDefaultValue(node["updateTunnelType"].ToString(), () => { proxy["TunnelType"] = int.Parse(node["updateTunnelType"].ToString()); });
                    RunAsNotDefaultValue(node["updateName"].ToString(), () => { proxy["TargetName"] = node["updateName"].GetValue<string>(); });
                    RunAsNotDefaultValue(node["updateListening"].ToString(), () => { proxy["ListenEnable"] = bool.Parse(node["updateListening"].ToString()); });
                    RunAsNotDefaultValue(node["updateIsPac"].ToString(), () => { proxy["IsPac"] = bool.Parse(node["updateIsPac"].ToString()); });
                    RunAsNotDefaultValue(node["updateIsConnect"].ToString(), () => { proxy["ConnectEnable"] = bool.Parse(node["updateIsConnect"].ToString()); });
                    RunAsNotDefaultValue(node["updateIsLanConnect"].ToString(), () => { proxy["LanConnectEnable"] = bool.Parse(node["updateIsLanConnect"].ToString()); });
                    RunAsNotDefaultValue(node["updateNum"].ToString(), () => { proxy["NumConnections"] = int.Parse(node["updateNum"].ToString()); });
                    RunAsNotDefaultValue(node["updateBufferSize"].ToString(), () => { proxy["BufferSize"] = int.Parse(node["updateBufferSize"].ToString()); });

                    Request("socks5/Set", proxy.ToJsonPipeline());
                    if (proxy["IsPac"].GetValue<bool>())
                    {
                        Request("socks5/SetPac", new { }.ToJsonPipeline());
                    }
                }

                PrintRequestState(res);
            }, new PersonBinder(updatePort, updateTunnelType, updateName, updateListening, updateIsPac, updateIsConnect, updateIsLanConnect, updateNum, updateBufferSize));


        }

    }


    public class PersonBinder : BinderBase<JsonNode>
    {
        private readonly Option<string> updatePort;
        private readonly Option<string> updateTunnelType;
        private readonly Option<string> updateName;
        private readonly Option<string> updateListening;
        private readonly Option<string> updateIsPac;
        private readonly Option<string> updateIsConnect;
        private readonly Option<string> updateIsLanConnect;
        private readonly Option<string> updateNum;
        private readonly Option<string> updateBufferSize;

        public PersonBinder(
            Option<string> updatePort, Option<string> updateTunnelType,
            Option<string> updateName, Option<string> updateListening,
            Option<string> updateIsPac, Option<string> updateIsConnect,
            Option<string> updateIsLanConnect, Option<string> updateNum,
            Option<string> updateBufferSize
            )
        {
            this.updatePort = updatePort;
            this.updateTunnelType = updateTunnelType;
            this.updateName = updateName;
            this.updateListening = updateListening;
            this.updateIsPac = updateIsPac;
            this.updateIsConnect = updateIsConnect;
            this.updateIsLanConnect = updateIsLanConnect;
            this.updateNum = updateNum;
            this.updateBufferSize = updateBufferSize;
        }

        protected override JsonNode GetBoundValue(BindingContext bindingContext)
        {
            JsonNode node = JsonNode.Parse("{}");

            node["updatePort"] = bindingContext.ParseResult.GetValueForOption(updatePort);
            node["updateTunnelType"] = bindingContext.ParseResult.GetValueForOption(updateTunnelType);
            node["updateName"] = bindingContext.ParseResult.GetValueForOption(updateName);
            node["updateListening"] = bindingContext.ParseResult.GetValueForOption(updateListening);
            node["updateIsPac"] = bindingContext.ParseResult.GetValueForOption(updateIsPac);
            node["updateIsConnect"] = bindingContext.ParseResult.GetValueForOption(updateIsConnect);
            node["updateIsLanConnect"] = bindingContext.ParseResult.GetValueForOption(updateIsLanConnect);
            node["updateNum"] = bindingContext.ParseResult.GetValueForOption(updateNum);
            node["updateBufferSize"] = bindingContext.ParseResult.GetValueForOption(updateBufferSize);

            return node;
        }

    }

    [Flags]
    public enum TunnelTypes : byte
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
