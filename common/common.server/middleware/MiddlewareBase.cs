using common.libs;
using System.Threading.Tasks;

namespace common.server.middleware
{
    /// <summary>
    /// 中间件
    /// </summary>
    public abstract class MiddlewareBase
    {
        /// <summary>
        /// 
        /// </summary>
        public MiddlewareBase Next { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public virtual async Task<(bool, byte[])> Execute(IConnection connection)
        {
            await Task.CompletedTask;
            return (true, Helper.EmptyArray);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MiddlewareTransfer
    {
        private MiddlewareBase first;
        private MiddlewareBase current;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
