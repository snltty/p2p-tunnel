using System;
using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace client.service.command.commands
{
    internal class CommandRegister : CommandBase
    {
        public CommandRegister(RootCommand rootCommand)
        {
            Command register = new Command("register", "注册相关命令") { };
            rootCommand.Add(register);

            Command start = new Command("start", "注册") { };
            register.Add(start);
            start.SetHandler(() =>
            {
                JsonNode res = JsonNode.Parse(Request("register/start"));
                PrintRequestState(res);
            });


            Command stop = new Command("stop", "离线") { };
            register.Add(stop);
            stop.SetHandler(() =>
            {
                JsonNode res = JsonNode.Parse(Request("register/stop"));
                PrintRequestState(res);
            });



            Command info = new Command("info", "配置信息") { };
            register.Add(info);
            info.SetHandler(() =>
            {
                Console.WriteLine(GetInfo()["Content"].ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) }));
            });



            Command config = new Command("config", "更新配置") { };
            register.Add(config);
            Option<string> configName = new Option<string>("--name", description: "客户端名称", getDefaultValue: getDefaultValue);
            config.AddOption(configName);
            Option<string> configGroupId = new Option<string>("--groupid", description: "分组编号", getDefaultValue: getDefaultValue);
            config.AddOption(configGroupId);
            Option<string> configAutoReg = new Option<string>("--autoreg", description: "是否自动注册", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            config.AddOption(configAutoReg);
            Option<string> configAutoRegTimes = new Option<string>("--autoreg-times", description: "自动注册重试次数", getDefaultValue: getDefaultValue);
            config.AddOption(configAutoRegTimes);
            Option<string> configServerAddress = new Option<string>("--ip", description: "服务端ip或域名", getDefaultValue: getDefaultValue);
            config.AddOption(configServerAddress);
            Option<string> configServerUdpPort = new Option<string>("--udp", description: "服务端udp端口", getDefaultValue: getDefaultValue);
            config.AddOption(configServerUdpPort);
            Option<string> configServerTcpPort = new Option<string>("--tcp", description: "服务端tcp端口", getDefaultValue: getDefaultValue);
            config.AddOption(configServerTcpPort);
            Option<string> configServerEncode = new Option<string>("--s-encode", description: "服务端通信加密", getDefaultValue: getDefaultValue).FromAmong("true", "false");
            config.AddOption(configServerEncode);
            config.SetHandler((name, configGroupId, configAutoReg, configAutoRegTimes, configServerAddress, configServerUdpPort, configServerTcpPort, configServerEncode) =>
            {
                JsonNode node = GetInfo();
                if (node.Root["Code"].GetValue<int>() == 0)
                {
                    JsonNode content = node.Root["Content"];
                    RunAsNotDefaultValue(name, () => { content["ClientConfig"]["Name"] = name; });
                    RunAsNotDefaultValue(configGroupId, () => { content["ClientConfig"]["GroupId"] = configGroupId; });
                    RunAsNotDefaultValue(configAutoReg, () => { content["ClientConfig"]["AutoReg"] = Boolean.Parse(configAutoReg); });
                    RunAsNotDefaultValue(configAutoRegTimes, () => { content["ClientConfig"]["AutoRegTimes"] = int.Parse(configAutoRegTimes); });
                    RunAsNotDefaultValue(configServerAddress, () => { content["ServerConfig"]["Ip"] = configServerAddress; });
                    RunAsNotDefaultValue(configServerUdpPort, () => { content["ServerConfig"]["UdpPort"] = int.Parse(configServerUdpPort); });
                    RunAsNotDefaultValue(configServerTcpPort, () => { content["ServerConfig"]["TcpPort"] = int.Parse(configServerTcpPort); });
                    RunAsNotDefaultValue(configServerEncode, () => { content["ServerConfig"]["Encode"] = Boolean.Parse(configServerEncode); });

                    JsonNode res = JsonNode.Parse(Request("register/config", content.ToJsonPipeline()));
                    PrintRequestState(node);
                }
                else
                {
                    PrintRequestState(node);
                }
            }, configName, configGroupId, configAutoReg, configAutoRegTimes, configServerAddress, configServerUdpPort, configServerTcpPort, configServerEncode);

        }

        private JsonNode GetInfo()
        {
            return JsonNode.Parse(Request("register/info"));
        }
    }
}
