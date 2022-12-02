using System;
using System.Collections.Generic;

namespace common.libs
{
    public sealed class SimpleSubPushHandler<T>
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
            if (actions.Count == 1)
            {
                actions[0](data);
            }
            else
            {
                for (int i = 0, len = actions.Count; i < len; i++)
                {
                    actions[i](data);
                }
            }
        }
        public void Remove(Action<T> action)
        {
            actions.Remove(action);
        }

    }
}
