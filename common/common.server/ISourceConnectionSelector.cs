namespace common.server
{
    public interface ISourceConnectionSelector
    {
        public IConnection SelectSource(IConnection connection, ulong relayid);
        public IConnection SelectTarget(IConnection connection, ulong fromid, ulong toid);
    }

    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        public IConnection SelectSource(IConnection connection, ulong relayid) { return connection; }
        public IConnection SelectTarget(IConnection connection, ulong fromid, ulong toid) { return connection; }
    }
}
