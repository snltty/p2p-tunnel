namespace common.server
{
    public interface ISourceConnectionSelector
    {
        public IConnection SelectSource(IConnection connection, ulong relayid);
        public IConnection SelectTarget(IConnection connection, ulong relayid);
    }

    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        public IConnection SelectSource(IConnection connection, ulong relayid) { return connection; }
        public IConnection SelectTarget(IConnection connection, ulong relayid) { return connection; }
    }
}
