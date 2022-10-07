using common.libs;
using System.Threading.Tasks;

namespace common.server.middleware
{
    public abstract class MiddlewareBase
    {
        public MiddlewareBase Next { get; set; }

        public virtual async Task<(bool, byte[])> Execute(IConnection connection)
        {
            await Task.CompletedTask;
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
        public async Task<(bool, byte[])> Execute(IConnection connection)
        {
            if (first != null)
            {
                MiddlewareBase execute = first;
                while (execute != null)
                {
                    var res = await execute.Execute(connection);
                    if (res.Item1 == false)
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
