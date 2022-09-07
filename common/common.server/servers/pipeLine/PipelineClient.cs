using System.IO;
using System.IO.Pipes;

namespace common.server.servers.pipeLine
{
    public class PipelineClient
    {
        private NamedPipeClientStream Client { get; set; }
        private StreamWriter Writer { get; set; }
        private StreamReader Reader { get; set; }

        public PipelineClient(string pipeName)
        {
            Client = new NamedPipeClientStream(".",pipeName);
            Writer = new StreamWriter(Client);
            Reader = new StreamReader(Client);

        }

        public void Connect()
        {
            Client.Connect();
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

        public void Dispose()
        {
            Client.Dispose();
            Client = null;

            Reader = null;

            Writer = null;
        }
    }
}
