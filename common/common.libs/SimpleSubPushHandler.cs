using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common.libs
{
    public class SimpleSubPushHandler<T>
    {
        List<Action<T>> actions = new List<Action<T>>();

        public void Sub(Action<T> action)
        {
            if (!actions.Contains(action))
            {
                actions.Add(action);
            }
        }
        public void Push(T data)
        {
            for (int i = 0, len = actions.Count; i < len; i++)
            {
                actions[i](data);
            }
        }
        public void Remove(Action<T> action)
        {
            actions.Remove(action);
        }

    }
}
