using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace common.server.servers.pipeLine
{
    public class PipelineServer
    {
        private NamedPipeServerStream Server { get; set; }
        private StreamWriter Writer { get; set; }
        private StreamReader Reader { get; set; }
        private Func<string, string> Action { get; set; }
        private string PipeName { get; set; }
        private static int maxNumberAcceptedClients = 5;

        public PipelineServer(string pipeName, Func<string, string> action)
        {
            Server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 254);
            Writer = new StreamWriter(Server);
            Reader = new StreamReader(Server);
            Action = action;
            PipeName = pipeName;


        }
        public void BeginAccept()
        {
            IAsyncResult result = Server.BeginWaitForConnection(ProcessAccept, null);
            if (result.CompletedSynchronously)
            {
                ProcessAccept(result);
            }
        }
        private void ProcessAccept(IAsyncResult result)
        {
            Server.EndWaitForConnection(result);

            Interlocked.Decrement(ref maxNumberAcceptedClients);
            if(maxNumberAcceptedClients > 0)
            {
                PipelineServer server = new PipelineServer(PipeName, Action);
                server.BeginAccept();
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        string msg = await Reader.ReadLineAsync().ConfigureAwait(false);
                        string res = Action(msg);
                        await Writer.WriteLineAsync(res).ConfigureAwait(false);
                        await Writer.FlushAsync();
                    }
                    catch (Exception)
                    {
                        Server.Disconnect();
                        break;
                    }
                }
                BeginAccept();
            });
        }

        public void Dispose()
        {
            Server.Dispose();
            Server = null;
            Writer = null;
            Reader = null;
            Action = null;
        }
    }

}
