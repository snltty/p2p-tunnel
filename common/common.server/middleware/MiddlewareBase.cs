using common.libs;

namespace common.server.middleware
{
    public abstract class MiddlewareBase
    {
        public MiddlewareBase Next { get; set; }

        public virtual (bool, byte[]) Execute(IConnection connection)
        {
            return (true, Helper.EmptyArray);
        }
    }

    public class MiddlewareTransfer
    {
        private MiddlewareBase first;
        private MiddlewareBase current;

        public void Load(MiddlewareBase type)
        {
            if (first == null)
            {
                first = type;
            }
            else
            {
                current.Next = type;
            }
            current = type;
        }
        public (bool, byte[]) Execute(IConnection connection)
        {
            if (first != null)
            {
                MiddlewareBase execute = first;
                while (execute != null)
                {
                    var res = execute.Execute(connection);
                    if (!res.Item1)
                    {
                        return res;
                    }
                    execute = execute.Next;
                }
            }
            return (true, Helper.EmptyArray);
        }
    }
}
