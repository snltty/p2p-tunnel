using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Unicode;

namespace client.service.command
{
    internal class PipelineClient
    {
        public NamedPipeClientStream Client { get; private set; }
        public StreamWriter Writer { get; private set; }
        public StreamReader Reader { get; private set; }

        public PipelineClient(string pipeName)
        {
            Client = new NamedPipeClientStream(pipeName);
            Writer = new StreamWriter(Client);
            Reader = new StreamReader(Client);

        }

        public void WriteLine(string msg)
        {
            Writer.WriteLine(msg);
            Writer.Flush();
        }
        public string ReadLine()
        {
            return Reader.ReadLine();
        }
        public void Connect()
        {
            Client.Connect();
        }

        public void Dispose()
        {
            Client.Dispose();
            Client = null;

            Reader = null;

            Writer = null;
        }
    }



    internal static class ObjectExtends
    {
        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true
        };
        public static string ToJsonPipeline(this object obj)
        {
            return JsonSerializer.Serialize(obj, options: jsonSerializerOptions);
        }
        public static T DeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, options: jsonSerializerOptions)!;
        }
    }
}
