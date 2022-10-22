namespace common.server
{
    public interface ISourceConnectionSelector
    {
        public IConnection Select(IConnection connection,ulong relayid);
    }

    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        public IConnection Select(IConnection connection, ulong relayid) { return connection; }
    }
}
