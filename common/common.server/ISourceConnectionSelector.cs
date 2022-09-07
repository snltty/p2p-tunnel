namespace common.server
{
    public interface ISourceConnectionSelector
    {
        public IConnection Select(IConnection connection);
    }

    public class SourceConnectionSelector : ISourceConnectionSelector
    {
        public IConnection Select(IConnection connection) { return connection; }
    }
}
