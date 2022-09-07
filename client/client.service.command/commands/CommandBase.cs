using ConsoleTableExt;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace client.service.command.commands
{
    internal class CommandBase
    {
        private static string pipeName = "client.cmd";
        private static readonly Lazy<PipelineClient> lazy = new Lazy<PipelineClient>(() => { PipelineClient client = new PipelineClient(pipeName); client.Connect(); return client; });
        public static PipelineClient Instance => lazy.Value;

        protected const string defaultValue = "---";
        protected Func<string> getDefaultValue = () => defaultValue;

        protected string Request(string path, string content = "")
        {
            Instance.WriteLine(new ClientServiceRequestInfo { Path = path, Content = content }.ToJsonPipeline());
            return Instance.ReadLine();
        }
        protected void RunAsNotDefaultValue(string value, Action action)
        {
            if (value != defaultValue)
            {
                action();
            }
        }

        protected void PrintRequestState(JsonNode node)
        {
            ConsoleColor currentForeColor = Console.ForegroundColor;
            if (node.Root["Code"].GetValue<int>() == 0)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("OK");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FAIL:{node.Root["Content"].GetValue<string>()}");
            }
            Console.ForegroundColor = currentForeColor;
        }
        protected void PrintTable(List<List<object>> data)
        {
            ConsoleTableBuilder.From(data)
                .WithFormat(ConsoleTableBuilderFormat.Alternative)
                .ExportAndWriteLine(TableAligntment.Left);
        }
    }

    internal class ClientServiceRequestInfo
    {
        public string Path { get; set; } = string.Empty;
        public long RequestId { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }
}
