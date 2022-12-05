using System.IO;
using System.IO.Pipes;

namespace common.server.servers.pipeLine
{
    /// <summary>
    /// 具名管道客户端
    /// </summary>
    public class PipelineClient
    {
        private NamedPipeClientStream Client { get; set; }
        private StreamWriter Writer { get; set; }
        private StreamReader Reader { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipeName"></param>
        public PipelineClient(string pipeName)
        {
            Client = new NamedPipeClientStream(".",pipeName);
            Writer = new StreamWriter(Client);
            Reader = new StreamReader(Client);

        }
        /// <summary>
        /// 
        /// </summary>
        public void Connect()
        {
            Client.Connect();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public void WriteLine(string msg)
        {
            Writer.WriteLine(msg);
            Writer.Flush();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            return Reader.ReadLine();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Client.Dispose();
            Client = null;

            Reader = null;

            Writer = null;
        }
    }
}
